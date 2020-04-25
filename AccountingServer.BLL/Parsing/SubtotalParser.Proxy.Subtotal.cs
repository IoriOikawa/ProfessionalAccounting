using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;

namespace AccountingServer.BLL.Parsing
{
    internal partial class SubtotalParser
    {
        public partial class SubtotalContext : ISubtotal
        {
            /// <inheritdoc />
            public GatheringType GatherType
                => Mark.Text switch
                    {
                        "``" => GatheringType.Zero,
                        "`" => GatheringType.NonZero,
                        "!" => GatheringType.Count,
                        "!!" => GatheringType.VoucherCount,
                        _ => throw new MemberAccessException("表达式错误"),
                    };

            /// <inheritdoc />
            public IReadOnlyList<SubtotalLevel> Levels
                => SubtotalFields()?.GetText() switch
                    {
                        null when subtotalEqui() == null => new[]
                            {
                                SubtotalLevel.Currency,
                                SubtotalLevel.Title,
                                SubtotalLevel.SubTitle,
                                SubtotalLevel.User,
                                SubtotalLevel.Content,
                            },
                        null => new[]
                            {
                                SubtotalLevel.Title,
                                SubtotalLevel.SubTitle,
                                SubtotalLevel.User,
                                SubtotalLevel.Content,
                            },
                        "v" => new SubtotalLevel[0],
                        _ => SubtotalFields()?.GetText().Select(ch => ch switch
                                {
                                    't' => SubtotalLevel.Title,
                                    's' => SubtotalLevel.SubTitle,
                                    'c' => SubtotalLevel.Content,
                                    'r' => SubtotalLevel.Remark,
                                    'C' => SubtotalLevel.Currency,
                                    'U' => SubtotalLevel.User,
                                    'd' => SubtotalLevel.Day,
                                    'w' => SubtotalLevel.Week,
                                    'm' => SubtotalLevel.Month,
                                    'y' => SubtotalLevel.Year,
                                    _ => throw new MemberAccessException("表达式错误"),
                                })
                            .ToList(),
                    };

            /// <inheritdoc />
            public AggregationType AggrType
                => subtotalAggr() switch
                    {
                        null => AggregationType.None,
                        var x when x.AllDate() == null && x.rangeCore() == null => AggregationType.ChangedDay,
                        _ => AggregationType.EveryDay,
                    };

            /// <inheritdoc />
            public SubtotalLevel AggrInterval
                => subtotalAggr() switch
                    {
                        null => SubtotalLevel.None,
                        var x => x.AggrMark().GetText() switch
                            {
                                "D" => SubtotalLevel.Day,
                                "W" => SubtotalLevel.Week,
                                "M" => SubtotalLevel.Month,
                                "Y" => SubtotalLevel.Year,
                                _ => throw new MemberAccessException("表达式错误"),
                            },
                    };

            /// <inheritdoc />
            public IDateRange EveryDayRange
                => subtotalAggr() switch
                    {
                        var x when x.AllDate() != null => DateFilter.Unconstrained,
                        _ => subtotalAggr().rangeCore(),
                    };

            /// <inheritdoc />
            public string EquivalentCurrency
                => subtotalEqui() switch
                    {
                        null => null,
                        var x => x.VoucherCurrency()?.GetText().ParseCurrency() ?? BaseCurrency.Now,
                    };

            /// <inheritdoc />
            public DateTime? EquivalentDate
                => subtotalEqui() switch
                    {
                        null => (DateTime?)null,
                        var x => x.rangeDay() ?? ClientDateTime.Today,
                    };
        }
    }
}
