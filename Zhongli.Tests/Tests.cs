using AutoMapper;
using Xunit;
using Zhongli.Data.Models.Core;

namespace Zhongli.Tests
{
    public class Tests
    {
        [Fact]
        public void Test1() { Assert.True(true); }

        [Fact]
        public void TestConfiguraiton()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<GenericRequest, GenericRequestDto>();
            });

            configuration.AssertConfigurationIsValid();
            var mapper = configuration.CreateMapper();
        }
    }
}