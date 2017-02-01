﻿using System;
using System.Collections.Generic;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using static AccountingServer.BLL.Parsing.Facade;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     实体表达式
    /// </summary>
    public class ExpressionHelper : IEntitySerializer
    {
        /// <inheritdoc />
        public string PresentVoucher(Voucher voucher)
        {
            var sb = new StringBuilder();
            sb.Append("@new Voucher {");
            sb.AppendLine(voucher.ID?.Quotation('^'));
            sb.AppendLine(voucher.Date.AsDate());
            if (voucher.Remark != null)
                sb.AppendLine(voucher.Remark.Quotation('%'));
            if (voucher.Type.HasValue && voucher.Type != VoucherType.Ordinary)
                sb.AppendLine(voucher.Type.ToString());
            if (voucher.Currency != null &&
                voucher.Currency != Voucher.BaseCurrency)
                sb.AppendLine($"@{voucher.Currency}");

            foreach (var d in voucher.Details)
            {
                var t = TitleManager.GetTitleName(d.Title);
                var s = TitleManager.GetTitleName(d.Title);
                sb.AppendLine($"// {t}-{s}");
                sb.AppendLine($"T{d.Title.AsTitle()}{d.SubTitle.AsSubTitle()} {d.Content?.Quotation('\'')} {d.Remark?.Quotation('\"')} {d.Fund}");
            }

            sb.AppendLine("}@");
            return sb.ToString();
        }

        /// <inheritdoc />
        public Voucher ParseVoucher(string expr)
        {
            Parsing.TrimStartComment(ref expr);
            var id = Parsing.Quoted(ref expr, '^');
            Parsing.TrimStartComment(ref expr);
            var date = Parsing.UniqueTime(ref expr) ?? DateTime.Today; // TODO
            Parsing.TrimStartComment(ref expr);
            var remark = Parsing.Quoted(ref expr, '%');
            Parsing.TrimStartComment(ref expr);
            var typeT = VoucherType.Ordinary;
            var type = Parsing.Token(ref expr, false, t => Enum.TryParse(t, out typeT)) != null ? (VoucherType?)typeT : null;
            Parsing.TrimStartComment(ref expr);
            var currency = Parsing.Token(ref expr, false, t => t.StartsWith("@", StringComparison.Ordinal))?.ToUpperInvariant();

            var lst = new List<VoucherDetail>();
            VoucherDetail d;
            while ((d = GetVoucherDetail(ref expr)) != null)
                lst.Add(d);

            return new Voucher
                {
                    ID = id,
                    Remark = remark,
                    Type = type,
                    Currency = currency,
                    Date = date,
                    Details = lst
                };
        }

        /// <summary>
        ///     解析细目表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>细目</returns>
        private static VoucherDetail GetVoucherDetail(ref string expr)
        {
            Parsing.TrimStartComment(ref expr);
            var title = Parsing.Title(ref expr);
            if (title == null)
                return null;

            var lst = new List<string>();
            double? fund;

            while (true)
            {
                Parsing.TrimStartComment(ref expr);
                if ((fund = Parsing.Double(ref expr)) != null)
                    break;

                Parsing.TrimStartComment(ref expr);
                if (Parsing.Optional(ref expr, "null"))
                    break;

                if (lst.Count > 2)
                    throw new ArgumentException("语法错误", nameof(expr));

                Parsing.TrimStartComment(ref expr);
                lst.Add(Parsing.Token(ref expr));
            }

            var content = lst.Count >= 1 ? lst[0] : null;
            var remark = lst.Count >= 2 ? lst[1] : null;

            return new VoucherDetail
                {
                    Title = title.Title,
                    SubTitle = title.SubTitle,
                    Content = content,
                    Fund = fund,
                    Remark = remark
                };
        }

        public string PresentAsset(Asset asset) { throw new NotImplementedException(); }
        public Asset ParseAsset(string str) { throw new NotImplementedException(); }
        public string PresentAmort(Amortization amort) { throw new NotImplementedException(); }
        public Amortization ParseAmort(string str) { throw new NotImplementedException(); }
    }
}
