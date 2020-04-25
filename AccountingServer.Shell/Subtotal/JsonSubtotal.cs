using System;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using AccountingServer.Shell.Serializer;
using Newtonsoft.Json.Linq;

namespace AccountingServer.Shell.Subtotal
{
    /// <summary>
    ///     分类汇总结果导出
    /// </summary>
    internal class JsonSubtotal : ISubtotalVisitor<JProperty>, ISubtotalStringify
    {
        private int m_Depth;

        private ISubtotal m_Par;

        /// <inheritdoc />
        public string PresentSubtotal(ISubtotalResult raw, ISubtotal par, IEntitiesSerializer serializer)
        {
            m_Par = par;
            m_Depth = 0;
            return (raw?.Accept(this)?.Value as JObject)?.ToString();
        }

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalRoot sub)
            => new JProperty("", VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalDate sub)
            => new JProperty(sub.Date.AsDate(sub.Level), VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalUser sub)
            => new JProperty(sub.User, VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalCurrency sub)
            => new JProperty(sub.Currency, VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalTitle sub)
            => new JProperty(sub.Title.AsTitle(), VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalSubTitle sub)
            => new JProperty(sub.SubTitle.AsSubTitle(), VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalContent sub)
            => new JProperty(sub.Content ?? "", VisitChildren(sub));

        JProperty ISubtotalVisitor<JProperty>.Visit(ISubtotalRemark sub)
            => new JProperty(sub.Remark ?? "", VisitChildren(sub));

        private JObject VisitChildren(ISubtotalResult sub)
        {
            var obj = new JObject(new JProperty("value", sub.Fund));
            if (sub.Items == null)
                return obj;

            var field = m_Depth < m_Par.Levels.Count
                ? m_Par.Levels[m_Depth] switch
                    {
                        SubtotalLevel.Title => "title",
                        SubtotalLevel.SubTitle => "subtitle",
                        SubtotalLevel.Content => "content",
                        SubtotalLevel.Remark => "remark",
                        SubtotalLevel.User => "user",
                        SubtotalLevel.Currency => "currency",
                        SubtotalLevel.Day => "date",
                        SubtotalLevel.Week => "date",
                        SubtotalLevel.Month => "date",
                        SubtotalLevel.Year => "date",
                        _ => throw new ArgumentOutOfRangeException(),
                    }
                : "aggr";

            m_Depth++;
            obj[field] = new JObject(sub.Items.Select(it => it.Accept(this)));
            m_Depth--;

            return obj;
        }
    }
}
