using System;
using System.Collections.Generic;
using System.Linq;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.Test
{
    public class DistributedEqualityComparer : IEqualityComparer<IDistributed>
    {
        public bool Equals(IDistributed x, IDistributed y)
        {
            if (x == null &&
                y == null)
                return true;
            if (x == null ||
                y == null)
                return false;
            if (x.ID != y.ID)
                return false;
            if (x.User != y.User)
                return false;
            if (x.Name != y.Name)
                return false;
            if (x.Date != y.Date)
                return false;
            if (x.Value.HasValue != y.Value.HasValue)
                return false;
            if (x.Value.HasValue &&
                y.Value.HasValue)
                if (!(x.Value.Value - y.Value.Value).IsZero())
                    return false;
            if (x.Remark != y.Remark)
                return false;

            return true;
        }

        public int GetHashCode(IDistributed obj) => obj.ID?.GetHashCode() ?? 0;
    }

    public class AssetItemEqualityComparer : DistributedItemEqualityComparer, IEqualityComparer<AssetItem>
    {
        public bool Equals(AssetItem x0, AssetItem y0)
            => base.Equals(x0, y0) && (x0, y0) switch
                {
                    (AcquisitionItem x, AcquisitionItem y) => (x.OrigValue - y.OrigValue).IsZero(),
                    (DepreciateItem x, DepreciateItem y) => (x.Amount - y.Amount).IsZero(),
                    (DevalueItem x, DevalueItem y)
                    => (x.FairValue - y.FairValue).IsZero() && (x.Amount - y.Amount).IsZero(),
                    (DispositionItem _, DispositionItem _) => true,
                    _ => throw new InvalidOperationException(),
                };

        public int GetHashCode(AssetItem obj) => base.GetHashCode(obj);
    }

    public class AssetEqualityComparer : DistributedEqualityComparer, IEqualityComparer<Asset>
    {
        public bool Equals(Asset x, Asset y)
        {
            if (!base.Equals(x, y))
                return false;
            if (x.Currency != y.Currency)
                return false;
            if (x.Salvage.HasValue != y.Salvage.HasValue)
                return false;
            if (x.Salvage.HasValue &&
                y.Salvage.HasValue)
                if (!(x.Salvage.Value - y.Salvage.Value).IsZero())
                    return false;
            if (x.Life != y.Life)
                return false;
            if (x.Title != y.Title)
                return false;
            if (x.DepreciationTitle != y.DepreciationTitle)
                return false;
            if (x.DevaluationTitle != y.DevaluationTitle)
                return false;
            if (x.DepreciationExpenseTitle != y.DepreciationExpenseTitle)
                return false;
            if (x.DepreciationExpenseSubTitle != y.DepreciationExpenseSubTitle)
                return false;
            if (x.DevaluationExpenseTitle != y.DevaluationExpenseTitle)
                return false;
            if (x.DevaluationExpenseSubTitle != y.DevaluationExpenseSubTitle)
                return false;
            if (x.Method != y.Method)
                return false;

            return x.Schedule.SequenceEqual(y.Schedule, new AssetItemEqualityComparer());
        }

        public int GetHashCode(Asset obj) => base.GetHashCode(obj);
    }

    public class AmortItemEqualityComparer : DistributedItemEqualityComparer, IEqualityComparer<AmortItem>
    {
        public bool Equals(AmortItem x, AmortItem y)
        {
            if (!base.Equals(x, y))
                return false;

            if (!(x.Amount - y.Amount).IsZero())
                return false;

            return true;
        }

        public int GetHashCode(AmortItem obj) => base.GetHashCode(obj);
    }

    public class AmortEqualityComparer : DistributedEqualityComparer, IEqualityComparer<Amortization>
    {
        private readonly VoucherEqualityComparer m_Comparer = new VoucherEqualityComparer();

        public bool Equals(Amortization x, Amortization y)
        {
            if (!base.Equals(x, y))
                return false;
            if (x.TotalDays != y.TotalDays)
                return false;
            if (x.Interval != y.Interval)
                return false;
            if (!m_Comparer.Equals(x.Template, y.Template))
                return false;

            return x.Schedule.SequenceEqual(y.Schedule, new AmortItemEqualityComparer());
        }

        public int GetHashCode(Amortization obj) => base.GetHashCode(obj);
    }
}
