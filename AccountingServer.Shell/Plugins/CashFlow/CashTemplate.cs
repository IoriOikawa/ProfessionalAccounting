﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AccountingServer.Shell.Plugins.CashFlow
{
    [Serializable]
    [XmlRoot("Templates")]
    public class CashTemplates
    {
        [XmlElement]
        public string QuickAsset { get; set; }

        [XmlElement]
        public bool Reimburse { get; set; }

        [XmlArray("Items")]
        [XmlArrayItem("FixedItem", typeof(FixedItem))]
        [XmlArrayItem("SimpleItem", typeof(SimpleItem))]
        [XmlArrayItem("CreditCard", typeof(CreditCard))]
        public List<CashFlowItem> Items;
    }

    [Serializable]
    public abstract class CashFlowItem { }

    [Serializable]
    public class FixedItem : CashFlowItem
    {
        [XmlAttribute("day")]
        public DateTime Day { get; set; }

        [XmlAttribute("fund")]
        public double Fund { get; set; }
    }

    [Serializable]
    public class SimpleItem : CashFlowItem
    {
        [XmlAttribute("day")]
        public DateTime Day { get; set; }

        [XmlText]
        public string Query { get; set; }
    }

    [Serializable]
    public class CreditCard : CashFlowItem
    {
        [XmlAttribute("repay")]
        public int RepaymentDay { get; set; }

        [XmlText]
        public string Query { get; set; }

        [XmlAttribute("bill")]
        public int BillDay { get; set; }
    }
}