using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace AccountingServer.DAL
{
    internal class VoucherSerializer : BsonBaseSerializer
    {
        public override object Deserialize(BsonReader bsonReader, Type nominalType, Type actualType,
                                           IBsonSerializationOptions options)
        {
            string read = null;
            bsonReader.ReadStartDocument();

            var voucher = new Voucher
                              {
                                  ID = bsonReader.ReadObjectId("_id", ref read),
                                  Date = bsonReader.ReadDateTime("date", ref read),
                                  Type = VoucherType.Ordinal,
                              };
            switch (bsonReader.ReadString("special", ref read))
            {
                case "amorz":
                    voucher.Type = VoucherType.Amortization;
                    break;
                case "acarry":
                    voucher.Type = VoucherType.AnnualCarry;
                    break;
                case "carry":
                    voucher.Type = VoucherType.Carry;
                    break;
                case "dep":
                    voucher.Type = VoucherType.Depreciation;
                    break;
                case "dev":
                    voucher.Type = VoucherType.Devalue;
                    break;
                case "unc":
                    voucher.Type = VoucherType.Uncertain;
                    break;
            }
            voucher.Details = bsonReader.ReadArray("detail", ref read, VoucherDetailSerializer.Deserialize).ToArray();
            voucher.Remark = bsonReader.ReadString("remark", ref read);
            bsonReader.ReadEndDocument();

            return voucher;
        }

        public override void Serialize(BsonWriter bsonWriter, Type nominalType, object value,
                                       IBsonSerializationOptions options)
        {
            var voucher = (Voucher)value;

            bsonWriter.WriteStartDocument();
            bsonWriter.WriteObjectId("_id", voucher.ID);
            bsonWriter.Write("date", voucher.Date);
            if (voucher.Type != VoucherType.Ordinal)
                switch (voucher.Type)
                {
                    case VoucherType.Amortization:
                        bsonWriter.Write("special", "amorz");
                        break;
                    case VoucherType.AnnualCarry:
                        bsonWriter.Write("special", "acarry");
                        break;
                    case VoucherType.Carry:
                        bsonWriter.Write("special", "carry");
                        break;
                    case VoucherType.Depreciation:
                        bsonWriter.Write("special", "dep");
                        break;
                    case VoucherType.Devalue:
                        bsonWriter.Write("special", "dev");
                        break;
                    case VoucherType.Uncertain:
                        bsonWriter.Write("special", "unc");
                        break;
                }
            if (voucher.Details != null)
            {
                bsonWriter.WriteStartArray("detail");
                foreach (var detail in voucher.Details)
                    VoucherDetailSerializer.Serialize(bsonWriter, detail);
                bsonWriter.WriteEndArray();
            }
            if (voucher.Remark != null)
                bsonWriter.WriteString("remark", voucher.Remark);
            bsonWriter.WriteEndDocument();
        }
    }
}