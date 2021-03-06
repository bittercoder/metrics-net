﻿using System;
using System.Linq;
using metrics.Stats;
using NUnit.Framework;

namespace metrics.Tests.Stats
{
    [TestFixture]
    public class LimitedTimeSampleTests
    {
        [SetUp]
        public void BeforeTest()
        {
            _dateTimeSupplier = new MockDateTimeSupplier(new DateTime(2012, 08, 13, 12, 30, 00));
            _underTest = new LimitedTimeSample(_dateTimeSupplier, TimeToKeepItems, TimeBetweenRemovingOldItems);
        }

        static readonly TimeSpan TimeToKeepItems = TimeSpan.FromMinutes(5);
        static readonly TimeSpan TimeBetweenRemovingOldItems = TimeSpan.FromMinutes(1);
        static MockDateTimeSupplier _dateTimeSupplier;
        static LimitedTimeSample _underTest;

        class MockDateTimeSupplier : IDateTimeSupplier
        {
            public MockDateTimeSupplier(DateTime dateTime)
            {
                UtcNow = dateTime;
            }

            public DateTime UtcNow { get; private set; }

            internal void SetNow(DateTime dateTime)
            {
                UtcNow = dateTime;
            }
        }

        [Test]
        public void Clear_ClearsAllItems()
        {
            _underTest.Update(8);
            Assert.AreNotEqual(0, _underTest.Count);
            _underTest.Clear();
            Assert.AreEqual(0, _underTest.Count);
        }

        [Test]
        public void Copy_CopiesItems()
        {
            _underTest.Update(9);
            _underTest.Update(10);
            LimitedTimeSample returned = _underTest.Copy;
            Assert.AreEqual(2, returned.Count);
            Assert.IsTrue(returned.Values.Contains(9));
            Assert.IsTrue(returned.Values.Contains(10));
        }

        [Test]
        public void Update_AddsItems()
        {
            _underTest.Update(8);
            Assert.AreEqual(1, _underTest.Count);
            Assert.AreEqual(8, _underTest.Values.First());
        }

        [Test]
        public void Update_WhenTimeBetweenRemovingOldItemsHasPassed_RemovesOldItems()
        {
            DateTime dateTime1 = _dateTimeSupplier.UtcNow;
            _dateTimeSupplier.SetNow(dateTime1);
            _underTest.Update(10);
            _dateTimeSupplier.SetNow(dateTime1.AddMinutes(2));
            _underTest.Update(11);
            Assert.AreEqual(2, _underTest.Count);
            _dateTimeSupplier.SetNow(dateTime1.AddMinutes(5).AddSeconds(1));
            _underTest.Update(12);
            Assert.AreEqual(2, _underTest.Count);
        }
    }
}