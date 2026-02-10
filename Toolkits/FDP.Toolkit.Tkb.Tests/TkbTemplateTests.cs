using Xunit;
using Fdp.Interfaces;
using System;
using System.Collections.Generic;

namespace Fdp.Toolkit.Tkb.Tests
{
    public class TkbTemplateTests
    {
        [Fact]
        public void Ctor_ValidatesArgs()
        {
            Assert.Throws<ArgumentNullException>(() => new TkbTemplate(null!, 1));
            Assert.Throws<ArgumentNullException>(() => new TkbTemplate("", 1));
            Assert.Throws<ArgumentException>(() => new TkbTemplate("Valid", 0));
        }

        [Fact]
        public void AreHardRequirementsMet_ReturnsTrue_WhenNoHardRequirements()
        {
            var template = new TkbTemplate("Test", 1);
            // Add soft req
            template.MandatoryDescriptors.Add(new MandatoryDescriptor { IsHard = false, PackedKey = 100 });
            
            bool result = template.AreHardRequirementsMet(new List<long>());
            Assert.True(result);
        }

        [Fact]
        public void AreHardRequirementsMet_ReturnsFalse_WhenHardRequirementMissing()
        {
             var template = new TkbTemplate("Test", 1);
             long key = PackedKey.Create(1, 1);
             template.MandatoryDescriptors.Add(new MandatoryDescriptor { IsHard = true, PackedKey = key });
             
             bool result = template.AreHardRequirementsMet(new List<long>());
             Assert.False(result);
        }

        [Fact]
        public void AreHardRequirementsMet_ReturnsTrue_WhenHardRequirementPresent()
        {
             var template = new TkbTemplate("Test", 1);
             long key = PackedKey.Create(1, 1);
             template.MandatoryDescriptors.Add(new MandatoryDescriptor { IsHard = true, PackedKey = key });
             
             bool result = template.AreHardRequirementsMet(new List<long> { key });
             Assert.True(result);
        }
    }
}
