using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     分类汇总结果处理器
    /// </summary>
    internal abstract class StringSubtotalVisitor : ISubtotalVisitor<Nothing>, ISubtotalStringify
    {
        protected string Cu;
        protected int Depth;

        protected GatheringType Ga;

        private ISubtotal m_Par;
        protected StringBuilder Sb;
        protected IEntitiesSerializer Serializer;

        /// <inheritdoc />
        public string PresentSubtotal(ISubtotalResult raw, ISubtotal par, IEntitiesSerializer serializer)
        {
            m_Par = par;
            Ga = par.GatherType;
            Cu = par.EquivalentCurrency;
            Serializer = serializer;
            Sb = new StringBuilder();
            Depth = 0;
            Pre();
            raw?.Accept(this);
            Post();
            return Sb.ToString();
        }

        public abstract Nothing Visit(ISubtotalRoot sub);
        public abstract Nothing Visit(ISubtotalDate sub);
        public abstract Nothing Visit(ISubtotalUser sub);
        public abstract Nothing Visit(ISubtotalCurrency sub);
        public abstract Nothing Visit(ISubtotalTitle sub);
        public abstract Nothing Visit(ISubtotalSubTitle sub);
        public abstract Nothing Visit(ISubtotalContent sub);
        public abstract Nothing Visit(ISubtotalRemark sub);

        protected virtual void Pre() { }
        protected virtual void Post() { }

        protected void VisitChildren(ISubtotalResult sub)
        {
            if (sub.Items == null)
                return;

            IEnumerable<ISubtotalResult> items;
            if (Depth < m_Par.Levels.Count)
            {
                var comparer = CultureInfo.GetCultureInfo("zh-CN").CompareInfo
                    .GetStringComparer(CompareOptions.StringSort);
                items = m_Par.Levels[Depth] switch
                    {
                        SubtotalLevel.Title => sub.Items.Cast<ISubtotalTitle>().OrderBy(s => s.Title),
                        SubtotalLevel.SubTitle => sub.Items.Cast<ISubtotalSubTitle>().OrderBy(s => s.SubTitle),
                        SubtotalLevel.Content => sub.Items.Cast<ISubtotalContent>().OrderBy(s => s.Content, comparer),
                        SubtotalLevel.Remark => sub.Items.Cast<ISubtotalRemark>().OrderBy(s => s.Remark, comparer),
                        SubtotalLevel.User => sub.Items.Cast<ISubtotalUser>()
                            .OrderBy(s => s.User == ClientUser.Name ? null : s.User),
                        SubtotalLevel.Currency => sub.Items.Cast<ISubtotalCurrency>()
                            .OrderBy(s => s.Currency == BaseCurrency.Now ? null : s.Currency),
                        SubtotalLevel.Day => sub.Items.Cast<ISubtotalDate>().OrderBy(s => s.Date),
                        SubtotalLevel.Week => sub.Items.Cast<ISubtotalDate>().OrderBy(s => s.Date),
                        SubtotalLevel.Month => sub.Items.Cast<ISubtotalDate>().OrderBy(s => s.Date),
                        SubtotalLevel.Year => sub.Items.Cast<ISubtotalDate>().OrderBy(s => s.Date),
                        _ => throw new ArgumentOutOfRangeException(),
                    };
            }
            else
                items = sub.Items;

            Depth++;
            foreach (var item in items)
                item.Accept(this);

            Depth--;
        }
    }
}
