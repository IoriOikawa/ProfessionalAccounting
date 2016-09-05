﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell;

namespace AccountingServer.Plugins.YieldRate
{
    /// <summary>
    ///     实际收益率计算
    /// </summary>
    public class YieldRate : PluginBase
    {
        public YieldRate(Accountant accountant, AccountingShell shell) : base(accountant, shell) { }

        /// <inheritdoc />
        public override IQueryResult Execute(params string[] pars)
        {
            var result =
                Accountant.SelectVoucherDetailsGrouped(
                                                       Shell.ParseGroupedQuery(
                                                                               "{T1101}-{T110102+T610101+T611102 A}:T1101``cd"));
            var resx = Accountant.SelectVoucherDetailsGrouped(Shell.ParseGroupedQuery("T1101``c"));
            var sb = new StringBuilder();
            foreach (
                var tpl in
                    result.GroupByContent()
                          .Join(
                                resx,
                                grp => grp.Key,
                                rsx => rsx.Content,
                                (grp, bal) => new Tuple<IGrouping<string, Balance>, double>(grp, bal.Fund)))
                sb.AppendLine(
                              $"{tpl.Item1.Key}\t{GetRate(tpl.Item1.OrderBy(b => b.Date, new DateComparer()).ToList(), tpl.Item2) * 360:P2}");
            return new UnEditableText(sb.ToString());
        }

        /// <summary>
        ///     计算实际收益率
        /// </summary>
        /// <param name="lst">现金流</param>
        /// <param name="pv">现值</param>
        /// <returns>实际收益率</returns>
        private static double GetRate(IReadOnlyList<Balance> lst, double pv)
        {
            // ReSharper disable PossibleInvalidOperationException
            if (!pv.IsZero())
                return
                    new YieldRateSolver(
                        lst.Select(b => DateTime.Today.Subtract(b.Date.Value).TotalDays).Concat(new[] { 0D }),
                        lst.Select(b => b.Fund).Concat(new[] { -pv })).Solve();
            return
                new YieldRateSolver(
                    lst.Select(b => lst.Last().Date.Value.Subtract(b.Date.Value).TotalDays),
                    lst.Select(b => b.Fund)).Solve();
            // ReSharper restore PossibleInvalidOperationException
        }
    }
}
