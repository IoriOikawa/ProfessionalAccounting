﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AccountingServer.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     MongoDb数据访问类
    /// </summary>
    public class MongoDbAdapter : IDbAdapter, IDbServer
    {
        #region Member

        /// <summary>
        ///     MongoDb客户端
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private MongoClient m_Client;

        /// <summary>
        ///     MongoDb服务器
        /// </summary>
        private MongoServer m_Server;

        /// <summary>
        ///     MongoDb数据库
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private MongoDatabase m_Db;

        /// <summary>
        ///     记账凭证集合
        /// </summary>
        private MongoCollection<Voucher> m_Vouchers;

        /// <summary>
        ///     资产集合
        /// </summary>
        private MongoCollection<Asset> m_Assets;

        /// <summary>
        ///     摊销集合
        /// </summary>
        private MongoCollection<Amortization> m_Amortizations;

        /// <summary>
        ///     命名查询模板集合
        /// </summary>
        private MongoCollection<BsonDocument> m_NamedQueryTemplates;

        #endregion

        public MongoDbAdapter() { Connected = false; }

        /// <summary>
        ///     注册Bson序列化器
        /// </summary>
        static MongoDbAdapter()
        {
            BsonSerializer.RegisterSerializer(typeof(Voucher), new VoucherSerializer());
            BsonSerializer.RegisterSerializer(typeof(VoucherDetail), new VoucherDetailSerializer());
            BsonSerializer.RegisterSerializer(typeof(Asset), new AssetSerializer());
            BsonSerializer.RegisterSerializer(typeof(AssetItem), new AssetItemSerializer());
            BsonSerializer.RegisterSerializer(typeof(Amortization), new AmortizationSerializer());
            BsonSerializer.RegisterSerializer(typeof(AmortItem), new AmortItemSerializer());
            BsonSerializer.RegisterSerializer(typeof(Balance), new BalanceSerializer());
        }

        #region Server

        /// <inheritdoc />
        public bool Connected { get; private set; }

        /// <inheritdoc />
        public void Launch()
        {
            var startinfo = new ProcessStartInfo
                                {
                                    FileName = "cmd.exe",
                                    Arguments =
                                        "/c " +
                                        "mongod --config \"C:\\Users\\b1f6c1c4\\Documents\\tjzh\\Account\\mongod.conf\"",
                                    UseShellExecute = false,
                                    RedirectStandardInput = false,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true
                                };

            var process = Process.Start(startinfo);
            if (process == null)
                throw new Exception();
        }

        /// <inheritdoc />
        public void Connect()
        {
            m_Client = new MongoClient("mongodb://localhost");
            m_Server = m_Client.GetServer();

            m_Db = m_Server.GetDatabase("accounting");

            m_Vouchers = m_Db.GetCollection<Voucher>("voucher");
            m_Assets = m_Db.GetCollection<Asset>("asset");
            m_Amortizations = m_Db.GetCollection<Amortization>("amortization");
            m_NamedQueryTemplates = m_Db.GetCollection<BsonDocument>("namedQuery");

            Connected = true;
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            if (!Connected)
                return;

            m_Db = null;
            m_Vouchers = null;
            m_Assets = null;
            m_Amortizations = null;
            m_NamedQueryTemplates = null;

            m_Server.Disconnect();
            m_Server = null;
            m_Client = null;

            Connected = false;
        }

        /// <inheritdoc />
        public void Backup()
        {
            var startinfo = new ProcessStartInfo
                                {
                                    FileName = "cmd.exe",
                                    Arguments =
                                        "/c " +
                                        "mongodump -h 127.0.0.1 -d accounting -o \"C:\\Users\\b1f6c1c4\\OneDrive\\Backup\"",
                                    UseShellExecute = false,
                                    RedirectStandardInput = false,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true
                                };

            var process = Process.Start(startinfo);
            if (process == null)
                throw new Exception();
        }

        #endregion

        #region Voucher

        /// <inheritdoc />
        public Voucher SelectVoucher(string id)
        {
            return m_Vouchers.FindOneById(ObjectId.Parse(id));
        }

        /// <inheritdoc />
        public IEnumerable<Voucher> SelectVouchers(IQueryCompunded<IVoucherQueryAtom> query)
        {
            return m_Vouchers.Find(query.GetQuery());
        }

        /// <inheritdoc />
        public IEnumerable<VoucherDetail> SelectVoucherDetails(IVoucherDetailQuery query)
        {
            return m_Vouchers.MapReduce(
                                        new MapReduceArgs
                                            {
                                                MapFunction =
                                                    new BsonJavaScript(GetMapJavascript(query, SubtotalLevel.None))
                                            }).GetResultsAs<VoucherDetail>();
        }

        /// <inheritdoc />
        public IEnumerable<Balance> SelectVoucherDetailsGrouped(IGroupedQuery query)
        {
            var level = query.Subtotal.Levels.Aggregate(SubtotalLevel.None, (total, l) => total | l);
            if (query.Subtotal.AggrType != AggregationType.None)
                level |= SubtotalLevel.Day;

            const string reduce =
                "function(key, values) { var total = 0; for (var i = 0; i < values.length; i++) total += values[i]; return total; }";
            var args = new MapReduceArgs
                           {
                               MapFunction = new BsonJavaScript(GetMapJavascript(query.VoucherEmitQuery, level)),
                               ReduceFunction = new BsonJavaScript(reduce)
                           };
            var res = m_Vouchers.MapReduce(args);
            return res.GetResultsAs<Balance>();
        }

        /// <inheritdoc />
        public bool DeleteVoucher(string id)
        {
            var res = m_Vouchers.Remove(MongoDbQueryHelper.GetQuery(id));
            return res.DocumentsAffected == 1;
        }

        /// <inheritdoc />
        public long DeleteVouchers(IQueryCompunded<IVoucherQueryAtom> query)
        {
            var res = m_Vouchers.Remove(query.GetQuery());
            return res.DocumentsAffected;
        }

        /// <inheritdoc />
        public bool Upsert(Voucher entity)
        {
            var res = m_Vouchers.Save(entity);
            return res.DocumentsAffected <= 1;
        }

        #endregion

        #region Asset

        /// <inheritdoc />
        public Asset SelectAsset(Guid id)
        {
            return m_Assets.FindOne(MongoDbQueryHelper.GetQuery(id));
        }

        /// <inheritdoc />
        public IEnumerable<Asset> SelectAssets(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            return m_Assets.Find(filter.GetQuery());
        }

        /// <inheritdoc />
        public bool DeleteAsset(Guid id)
        {
            var res = m_Assets.Remove(MongoDbQueryHelper.GetQuery(id));
            return res.DocumentsAffected == 1;
        }

        /// <inheritdoc />
        public bool Upsert(Asset entity)
        {
            var res = m_Assets.Save(entity);
            return res.DocumentsAffected == 1;
        }

        /// <inheritdoc />
        public long DeleteAssets(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            var res = m_Assets.Remove(filter.GetQuery());
            return res.DocumentsAffected;
        }

        #endregion

        #region Amortization

        /// <inheritdoc />
        public Amortization SelectAmortization(Guid id)
        {
            return m_Amortizations.FindOne(MongoDbQueryHelper.GetQuery(id));
        }

        /// <inheritdoc />
        public IEnumerable<Amortization> SelectAmortizations(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            return m_Amortizations.Find(filter.GetQuery());
        }

        /// <inheritdoc />
        public bool DeleteAmortization(Guid id)
        {
            var res = m_Amortizations.Remove(MongoDbQueryHelper.GetQuery(id));
            return res.DocumentsAffected == 1;
        }

        /// <inheritdoc />
        public bool Upsert(Amortization entity)
        {
            var res = m_Amortizations.Save(entity);
            return res.DocumentsAffected == 1;
        }

        /// <inheritdoc />
        public long DeleteAmortizations(IQueryCompunded<IDistributedQueryAtom> filter)
        {
            var res = m_Amortizations.Remove(filter.GetQuery());
            return res.DocumentsAffected;
        }

        #endregion

        #region NamedQurey

        /// <inheritdoc />
        public string SelectNamedQueryTemplate(string name)
        {
            return m_NamedQueryTemplates.FindOneById(new BsonString(name))["value"].AsString;
        }

        /// <inheritdoc />
        public IEnumerable<KeyValuePair<string, string>> SelectNamedQueryTemplates()
        {
            return
                m_NamedQueryTemplates.FindAll()
                                     .Select(
                                             d =>
                                             new KeyValuePair<string, string>(d["_id"].AsString, d["value"].AsString));
        }

        /// <inheritdoc />
        public bool DeleteNamedQueryTemplate(string name)
        {
            var res = m_NamedQueryTemplates.Remove(Query.EQ("_id", new BsonString(name)));
            return res.DocumentsAffected == 1;
        }

        /// <inheritdoc />
        public bool Upsert(string name, string value)
        {
            var res = m_NamedQueryTemplates.Save(
                                                 new BsonDocument
                                                     {
                                                         { "_id", name },
                                                         { "value", value }
                                                     });
            return res.DocumentsAffected == 1;
        }

        #endregion

        #region Javascript

        /// <summary>
        ///     根据分类要求，将日期进行转化
        /// </summary>
        /// <param name="subtotalLevel">分类汇总层次</param>
        /// <returns>转化的Javascript代码</returns>
        private static string GetTheDateJavascript(SubtotalLevel subtotalLevel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("var theDate = this.date;");
            if (!subtotalLevel.HasFlag(SubtotalLevel.Week))
                return sb.ToString();
            sb.AppendLine("if (theDate != null && theDate != undefined) {");
            sb.AppendLine("    theDate.setHours(0);");
            sb.AppendLine("    theDate.setMinutes(0);");
            sb.AppendLine("    theDate.setSeconds(0);");
            sb.AppendLine();
            if (subtotalLevel.HasFlag(SubtotalLevel.Year))
                sb.AppendLine("    theDate.setMonth(0, 1);");
            else if (subtotalLevel.HasFlag(SubtotalLevel.Month))
                sb.AppendLine("    theDate.setDate(1);");
            else if (subtotalLevel.HasFlag(SubtotalLevel.BillingMonth))
            {
                sb.AppendLine("    if (theDate.getDate() >= 9) {");
                sb.AppendLine("        theDate.setDate(9);");
                sb.AppendLine("        if (theDate.getMonth() == 11) {");
                sb.AppendLine("            theDate.setMonth(0);");
                sb.AppendLine("            theDate.setFullYear(theDate.getFullYear() + 1);");
                sb.AppendLine("        } else {");
                sb.AppendLine("            theDate.setMonth(theDate.getMonth() + 1);");
                sb.AppendLine("        }");
                sb.AppendLine("    } else {");
                sb.AppendLine("        theDate.setDate(9);");
                sb.AppendLine("    }");
            }
            else if (subtotalLevel.HasFlag(SubtotalLevel.FinancialMonth))
            {
                sb.AppendLine("    if (theDate.getDate() >= 20) {");
                sb.AppendLine("        theDate.setDate(19);");
                sb.AppendLine("        if (theDate.getMonth() == 11) {");
                sb.AppendLine("            theDate.setMonth(0);");
                sb.AppendLine("            theDate.setFullYear(theDate.getFullYear() + 1);");
                sb.AppendLine("        } else {");
                sb.AppendLine("            theDate.setMonth(theDate.getMonth() + 1);");
                sb.AppendLine("        }");
                sb.AppendLine("    } else {");
                sb.AppendLine("        theDate.setDate(19);");
                sb.AppendLine("    }");
            }
            else // if (subtotalLevel.HasFlag(SubtotalLevel.Week))
            {
                sb.AppendLine("    if (theDate.getDay() == 0)");
                sb.AppendLine("        theDate.setDate(theDate.getDate() - 6);");
                sb.AppendLine("    else");
                sb.AppendLine("        theDate.setDate(theDate.getDate() + 1 - theDate.getDay());");
            }
            sb.AppendLine("}");
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        ///     细目映射检索式的Javascript表示
        /// </summary>
        /// <param name="emitQuery">细目映射检索式</param>
        /// <returns>Javascript表示</returns>
        private static string GetEmitFilterJavascript(IEmit emitQuery)
        {
            if (emitQuery.DetailFilter == null)
                return "function(d) { return true; }";
            return emitQuery.DetailFilter.GetJavascriptFilter();
        }

        /// <summary>
        ///     映射函数的Javascript表示
        /// </summary>
        /// <param name="query">记账凭证检索式</param>
        /// <param name="subtotalLevel">分类汇总层次</param>
        /// <returns>Javascript表示</returns>
        private static string GetMapJavascript(IVoucherDetailQuery query, SubtotalLevel subtotalLevel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("function() {");
            sb.AppendLine("    var chk = ");
            if (query.DetailEmitFilter != null)
                sb.Append(GetEmitFilterJavascript(query.DetailEmitFilter));
            else
            {
                var dQuery = query.VoucherQuery as IVoucherQueryAtom;
                if (dQuery == null)
                    throw new InvalidOperationException();
                sb.Append(dQuery.DetailFilter.GetJavascriptFilter());
            }
            sb.AppendLine(";");
            sb.AppendLine("    if ((");
            sb.Append(query.VoucherQuery.GetJavascriptFilter());
            sb.AppendLine(")(this)) {");
            sb.AppendLine(GetTheDateJavascript(subtotalLevel));
            sb.AppendLine("        this.detail.forEach(function(d) {");
            sb.AppendLine("            if (chk(d))");
            {
                sb.Append("emit({");
                if (subtotalLevel.HasFlag(SubtotalLevel.Day))
                    sb.Append("date: theDate,");
                if (subtotalLevel.HasFlag(SubtotalLevel.Title))
                    sb.Append("title: d.title,");
                if (subtotalLevel.HasFlag(SubtotalLevel.SubTitle))
                    sb.Append("subtitle: d.subtitle,");
                if (subtotalLevel.HasFlag(SubtotalLevel.Content))
                    sb.Append("content: d.content,");
                if (subtotalLevel.HasFlag(SubtotalLevel.Remark))
                    sb.Append("remark: d.remark,");
                sb.Append("}, d.fund);");
            }
            sb.AppendLine("        });");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.Replace(Environment.NewLine, String.Empty).ToString();
        }

        #endregion
    }
}
