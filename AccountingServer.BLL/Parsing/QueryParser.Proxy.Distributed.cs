/* Copyright (C) 2020 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AccountingServer.BLL.Util;
using AccountingServer.Entities;
using static AccountingServer.Entities.Util.SecurityHelper;

namespace AccountingServer.BLL.Parsing
{
    internal partial class QueryParser
    {
        public partial class DistributedQAtomContext : IDistributedQueryAtom
        {
            /// <inheritdoc />
            public IDistributed Filter
                => new MyDistributedFilter
                    {
                        ID = Guid() != null ? System.Guid.Parse(Guid().GetText()) : (Guid?)null,
                        User = (UserSpec()?.GetText()).ParseUserSpec(),
                        Name = RegexString()?.GetText().Dequotation().Replace(@"\/", "/"),
                        Remark = PercentQuotedString()?.GetText().Dequotation(),
                    };

            /// <inheritdoc />
            public DateFilter Range => rangeCore()?.Range ?? DateFilter.Unconstrained;

            /// <inheritdoc />
            public bool IsDangerous() => Filter.IsDangerous() && Range.IsDangerous(true);

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);

            /// <summary>
            ///     分期过滤器
            /// </summary>
            [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
            private sealed class MyDistributedFilter : IDistributed
            {
                /// <inheritdoc />
                public Guid? ID { get; set; }

                /// <inheritdoc />
                public string User { get; set; }

                /// <inheritdoc />
                public string Name { get; set; }

                /// <inheritdoc />
                public DateTime? Date { get; set; }

                /// <inheritdoc />
                public double? Value { get; set; }

                /// <inheritdoc />
                public string Remark { get; set; }

                /// <inheritdoc />
                public IEnumerable<IDistributedItem> TheSchedule { get; set; }
            }
        }

        public partial class DistributedQContext : IQueryAry<IDistributedQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator
            {
                get
                {
                    if (Op == null)
                        return OperatorType.None;

                    if (distributedQ() == null)
                        switch (Op.Text)
                        {
                            case "+":
                                return OperatorType.Identity;
                            case "-":
                                return OperatorType.Complement;
                            default:
                                throw new MemberAccessException("表达式错误");
                        }

                    switch (Op.Text)
                    {
                        case "+":
                            return OperatorType.Union;
                        case "-":
                            return OperatorType.Subtract;
                        default:
                            throw new MemberAccessException("表达式错误");
                    }
                }
            }

            /// <inheritdoc />
            public IQueryCompounded<IDistributedQueryAtom> Filter1
            {
                get
                {
                    if (distributedQ() != null)
                        return distributedQ();

                    return distributedQ1();
                }
            }

            /// <inheritdoc />
            public IQueryCompounded<IDistributedQueryAtom> Filter2 => distributedQ1();

            /// <inheritdoc />
            public bool IsDangerous()
            {
                switch (Operator)
                {
                    case OperatorType.None:
                        return Filter1.IsDangerous();
                    case OperatorType.Identity:
                        return Filter1.IsDangerous();
                    case OperatorType.Complement:
                        return true;
                    case OperatorType.Union:
                        return Filter1.IsDangerous() || Filter2.IsDangerous();
                    case OperatorType.Subtract:
                        return Filter1.IsDangerous();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class DistributedQ1Context : IQueryAry<IDistributedQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => Op == null ? OperatorType.None : OperatorType.Intersect;

            /// <inheritdoc />
            public IQueryCompounded<IDistributedQueryAtom> Filter1 => distributedQ0();

            /// <inheritdoc />
            public IQueryCompounded<IDistributedQueryAtom> Filter2 => distributedQ1();

            /// <inheritdoc />
            public bool IsDangerous() => Filter1.IsDangerous() && (Filter2?.IsDangerous() ?? true);

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);
        }

        public partial class DistributedQ0Context : IQueryAry<IDistributedQueryAtom>
        {
            /// <inheritdoc />
            public OperatorType Operator => OperatorType.None;

            /// <inheritdoc />
            public IQueryCompounded<IDistributedQueryAtom> Filter1
                => (IQueryCompounded<IDistributedQueryAtom>)distributedQAtom() ?? distributedQ();

            /// <inheritdoc />
            public IQueryCompounded<IDistributedQueryAtom> Filter2 => null;

            /// <inheritdoc />
            public bool IsDangerous() => Filter1.IsDangerous();

            /// <inheritdoc />
            public T Accept<T>(IQueryVisitor<IDistributedQueryAtom, T> visitor) => visitor.Visit(this);
        }
    }
}
