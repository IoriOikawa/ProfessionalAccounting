﻿using System;
using System.Linq;
using AccountingServer.BLL;
using AccountingServer.Entities;
using AccountingServer.Shell.Parsing;

namespace AccountingServer.Shell
{
    /// <summary>
    ///     报告表达式解释器
    /// </summary>
    internal class ReportShell
    {
        /// <summary>
        ///     基本会计业务处理类
        /// </summary>
        private readonly Accountant m_Accountant;

        public ReportShell(Accountant helper) { m_Accountant = helper; }

        /// <summary>
        ///     执行报告表达式
        /// </summary>
        /// <param name="expr">表达式</param>
        /// <returns>执行结果</returns>
        public IQueryResult ExecuteReportQuery(ShellParser.ReportContext expr)
        {
            var rng = expr.range() != null ? expr.range().Range : ShellParser.From("[0]").range().Range;

            var helper = new NamedQueryTraver<string, string>(m_Accountant, rng)
                             {
                                 Leaf =
                                     (path, query, coefficient) =>
                                     PresentReport(
                                                   path.Length == 0 ? query.Name : path + "/" + query.Name,
                                                   query.GroupingQuery,
                                                   coefficient * query.Coefficient),
                                 Map = (path, query, coefficient) =>
                                       path.Length == 0 ? query.Name : path + "/" + query.Name,
                                 Reduce =
                                     (path, newPath, query, coefficient, results) => SubtotalHelper.NotNullJoin(results),
                             };

            if (expr.namedQuery() != null)
                return new UnEditableText(helper.Traversal(String.Empty, expr.namedQuery()));
            if (expr.groupedQuery() != null)
                return new UnEditableText(PresentReport(String.Empty, expr.groupedQuery(), 1));

            throw new ArgumentException("表达式类型未知", "expr");
        }

        /// <summary>
        ///     呈现报告条目
        /// </summary>
        /// <param name="path0">路径</param>
        /// <param name="query">分类汇总检索式</param>
        /// <param name="coefficient">路径上累计的系数</param>
        private string PresentReport(string path0, IGroupedQuery query, double coefficient)
        {
            var args = query.Subtotal;
            var res = m_Accountant.SelectVoucherDetailsGrouped(query);

            var helper =
                new SubtotalTraver<string, Tuple<double, string>>(args)
                    {
                        LeafNoneAggr =
                            (path, cat, depth, val) =>
                            new Tuple<double, string>(
                                val * coefficient,
                                String.Format("{0}\t{1:R}\t{2:R}\t{3:R}", path, val, coefficient, val * coefficient)),
                        LeafAggregated = (path, cat, depth, bal) =>
                            new Tuple<double, string>(
                                bal.Fund * coefficient,
                                String.Format("{0}\t{1:R}\t{2:R}\t{3:R}", path.Merge(bal.Date.AsDate()), bal.Fund, coefficient, bal.Fund * coefficient)),
                        Map = (path, cat, depth, level) =>
                              {
                                  switch (level)
                                  {
                                      case SubtotalLevel.Title:
                                          return path.Merge(TitleManager.GetTitleName(cat.Title));
                                      case SubtotalLevel.SubTitle:
                                          return path.Merge(TitleManager.GetTitleName(cat.Title, cat.SubTitle));
                                      case SubtotalLevel.Content:
                                          return path.Merge(cat.Content);
                                      case SubtotalLevel.Remark:
                                          return path.Merge(cat.Remark);
                                      default:
                                          return path.Merge(cat.Date.AsDate(level));
                                  }
                              },
                        MapA = (path, cat, depth, type) => path,
                        MediumLevel = (path, newPath, cat, depth, level, r) => r,
                        Reduce = (path, cat, depth, level, results) =>
                                 {
                                     var r = results.ToList();
                                     return new Tuple<double, string>(
                                         r.Sum(t => t.Item1),
                                         SubtotalHelper.NotNullJoin(r.Select(t => t.Item2)));
                                 },
                        ReduceA = (path, newPath, cat, depth, type, results) =>
                                  {
                                      var r = results.ToList();
                                      return new Tuple<double, string>(
                                          r.Sum(t => t.Item1),
                                          SubtotalHelper.NotNullJoin(r.Select(t => t.Item2)));
                                  }
                    };

            var traversal = helper.Traversal(path0, res);

            return traversal.Item2;
        }
    }
}
