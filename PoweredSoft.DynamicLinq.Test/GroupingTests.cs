﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoweredSoft.DynamicLinq;
using PoweredSoft.DynamicLinq.Dal;
using System.Diagnostics;
using PoweredSoft.DynamicLinq.Test.Helpers;

namespace PoweredSoft.DynamicLinq.Test
{
    internal class TestStructureCompare : IEqualityComparer<TestStructure> 
    {
        public bool Equals(TestStructure x, TestStructure y)
        {
            return x?.ClientId == y?.ClientId;
        }

        public int GetHashCode(TestStructure obj)
        {
            return obj.ClientId;
        }
    }

    internal class TestStructure 
    {
        public int ClientId { get; set; }
    }

    [TestClass]
    public class GroupingTests
    {
        [TestMethod]
        public void TestEmptyGroup()
        {
            var subject = TestData.Sales;

            var normalSyntax = subject
                .GroupBy(t => true)
                .Select(t => new
                {
                    NetSalesSum = t.Sum(t2 => t2.NetSales),
                    NetSalesAvg = t.Average(t2 => t2.NetSales)
                })
                .First();

            var dynamicSyntax = subject
                .EmptyGroupBy(typeof(MockSale))
                .Select(sb =>
                {
                    sb.Sum("NetSales", "NetSalesSum");
                    sb.Average("NetSales", "NetSalesAvg");
                })
                .ToDynamicClassList()
                .First();

            Assert.AreEqual(normalSyntax.NetSalesAvg, dynamicSyntax.GetDynamicPropertyValue<decimal>("NetSalesAvg"));
            Assert.AreEqual(normalSyntax.NetSalesSum, dynamicSyntax.GetDynamicPropertyValue<decimal>("NetSalesSum"));
        }

        [TestMethod]
        public void WantedSyntax()
        {
            var normalSyntax = TestData.Sales
                .GroupBy(t => new { t.ClientId })
                .Select(t => new
                {
                    TheClientId = t.Key.ClientId,
                    Count = t.Count(),
                    LongCount = t.LongCount(),
                    NetSales = t.Sum(t2 => t2.NetSales),
                    TaxAverage = t.Average(t2 => t2.Tax),
                    Sales = t.ToList()
                })
                .ToList();

            var dynamicSyntax = TestData.Sales
               .AsQueryable()
               .GroupBy(t => t.Path("ClientId"))
               .Select(t =>
               {
                   t.Key("TheClientId", "ClientId");
                   t.Count("Count");
                   t.LongCount("LongCount");
                   t.Sum("NetSales");
                   t.Average("Tax", "TaxAverage");
                   t.ToList("Sales");
               })
               .ToDynamicClassList();

            Assert.AreEqual(normalSyntax.Count, dynamicSyntax.Count);
            for(var i = 0; i < normalSyntax.Count; i++)
            {
                var left = normalSyntax[i];
                var right = dynamicSyntax[i];

                Assert.AreEqual(left.TheClientId, right.GetDynamicPropertyValue("TheClientId"));
                Assert.AreEqual(left.Count, right.GetDynamicPropertyValue("Count"));
                Assert.AreEqual(left.LongCount, right.GetDynamicPropertyValue("LongCount"));
                Assert.AreEqual(left.TaxAverage, right.GetDynamicPropertyValue("TaxAverage"));
                QueryableAssert.AreEqual(left.Sales.AsQueryable(), right.GetDynamicPropertyValue<List<MockSale>>("Sales").AsQueryable());
            }
        }

        [TestMethod]
        public void TestingSelectBuilderAggregateFluent()
        {
            var normalSyntax = TestData.Sales
                .GroupBy(t => new { t.ClientId })
                .Select(t => new
                {
                    TheClientId = t.Key.ClientId,
                    Count = t.Count(),
                    LongCount = t.LongCount(),
                    NetSales = t.Sum(t2 => t2.NetSales),
                    TaxAverage = t.Average(t2 => t2.Tax),
                    Sales = t.ToList()
                })
                .ToList();

            var dynamicSyntax = TestData.Sales
               .AsQueryable()
               .GroupBy(t => t.Path("ClientId"))
               .Select(t =>
               {
                   t.Aggregate("Key.ClientId", SelectTypes.Key, "TheClientId");
                   // should not have to specify a path, but a property is a must
                   t.Aggregate(null, SelectTypes.Count, "Count"); 
                   // support both ways it can use path to guess property so testing this too
                   t.Aggregate("LongCount", SelectTypes.LongCount); 
                   t.Aggregate("NetSales", SelectTypes.Sum);
                   t.Aggregate("Tax", SelectTypes.Average, "TaxAverage");
                   t.ToList("Sales");
               })
               .ToDynamicClassList();

            Assert.AreEqual(normalSyntax.Count, dynamicSyntax.Count);
            for (var i = 0; i < normalSyntax.Count; i++)
            {
                var left = normalSyntax[i];
                var right = dynamicSyntax[i];

                Assert.AreEqual(left.TheClientId, right.GetDynamicPropertyValue("TheClientId"));
                Assert.AreEqual(left.Count, right.GetDynamicPropertyValue("Count"));
                Assert.AreEqual(left.LongCount, right.GetDynamicPropertyValue("LongCount"));
                Assert.AreEqual(left.TaxAverage, right.GetDynamicPropertyValue("TaxAverage"));
                QueryableAssert.AreEqual(left.Sales.AsQueryable(), right.GetDynamicPropertyValue<List<MockSale>>("Sales").AsQueryable());
            }
        }
    }
}
