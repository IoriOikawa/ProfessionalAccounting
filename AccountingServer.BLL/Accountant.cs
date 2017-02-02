﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AccountingServer.DAL;
using AccountingServer.Entities;
using AccountingServer.Entities.Util;

namespace AccountingServer.BLL
{
    /// <summary>
    ///     基本会计业务处理类
    /// </summary>
    public class Accountant
    {
        /// <summary>
        ///     数据库访问
        /// </summary>
        private IDbAdapter m_Db;

        private readonly AssetAccountant m_AssetAccountant;

        private readonly AmortAccountant m_AmortAccountant;

        /// <summary>
        ///     获取是否已经连接到数据库
        /// </summary>
        public bool Connected => m_Db != null;

        public Accountant()
        {
            m_AssetAccountant = new AssetAccountant(m_Db);
            m_AmortAccountant = new AmortAccountant(m_Db);
        }

        #region Server

        public void Connect(string uri) => m_Db = Facade.Create(uri);

        #endregion

        #region Voucher

        public Voucher SelectVoucher(string id) => m_Db.SelectVoucher(id);

        public IEnumerable<Voucher> SelectVouchers(IQueryCompunded<IVoucherQueryAtom> query)
            => m_Db.SelectVouchers(query);

        public IEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query)
        {
            var res = m_Db.SelectVoucherDetailsGrouped(query);
            if (query.Subtotal.AggrType != AggregationType.ChangedDay &&
                query.Subtotal.GatherType == GatheringType.NonZero)
                return res.Where(b => !b.Fund.IsZero());

            return res;
        }

        public bool DeleteVoucher(string id) => m_Db.DeleteVoucher(id);

        public long DeleteVouchers(IQueryCompunded<IVoucherQueryAtom> query) => m_Db.DeleteVouchers(query);

        public bool Upsert(Voucher entity)
        {
            entity.Currency = entity.Currency?.ToUpperInvariant() ?? Voucher.BaseCurrency;
            return m_Db.Upsert(entity);
        }

        #endregion

        #region Asset

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public Asset SelectAsset(Guid id)
        {
            var result = m_Db.SelectAsset(id);
            AssetAccountant.InternalRegular(result);
            return result;
        }

        public IEnumerable<Asset> SelectAssets(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            foreach (var asset in m_Db.SelectAssets(filter))
            {
                AssetAccountant.InternalRegular(asset);
                yield return asset;
            }
        }

        public bool DeleteAsset(Guid id) => m_Db.DeleteAsset(id);

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public long DeleteAssets(IQueryCompunded<IDistributedQueryAtom> filter) => m_Db.DeleteAssets(filter);

        public bool Upsert(Asset entity) => m_Db.Upsert(entity);

        public IEnumerable<Voucher> RegisterVouchers(Asset asset, DateFilter rng,
            IQueryCompunded<IVoucherQueryAtom> query)
            => m_AssetAccountant.RegisterVouchers(asset, rng, query);

        public static void Depreciate(Asset asset) => AssetAccountant.Depreciate(asset);

        public IEnumerable<AssetItem> Update(Asset asset, DateFilter rng,
            bool isCollapsed = false, bool editOnly = false)
            => m_AssetAccountant.Update(asset, rng, isCollapsed, editOnly);

        #endregion

        #region Amort

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public Amortization SelectAmortization(Guid id)
        {
            var result = m_Db.SelectAmortization(id);
            AmortAccountant.InternalRegular(result);
            return result;
        }

        public IEnumerable<Amortization> SelectAmortizations(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            foreach (var amort in m_Db.SelectAmortizations(filter))
            {
                AmortAccountant.InternalRegular(amort);
                yield return amort;
            }
        }

        public bool DeleteAmortization(Guid id) => m_Db.DeleteAmortization(id);

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public long DeleteAmortizations(IQueryCompunded<IDistributedQueryAtom> filter)
            => m_Db.DeleteAmortizations(filter);

        public bool Upsert(Amortization entity) => m_Db.Upsert(entity);

        public IEnumerable<Voucher> RegisterVouchers(Amortization amort, DateFilter rng,
            IQueryCompunded<IVoucherQueryAtom> query)
            => m_AmortAccountant.RegisterVouchers(amort, rng, query);

        public static void Amortize(Amortization amort) => AmortAccountant.Amortize(amort);

        public IEnumerable<AmortItem> Update(Amortization amort, DateFilter rng,
            bool isCollapsed = false, bool editOnly = false)
            => m_AmortAccountant.Update(amort, rng, isCollapsed, editOnly);

        public static double? GetBookValueOn(IDistributed dist, DateTime? dt)
            => DistributedAccountant.GetBookValueOn(dist, dt);

        #endregion
    }
}
