﻿
namespace System.Data.Entity.Edm.Serialization
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Xml.Linq;
    using Xunit;

    public class CsdlSerializerTests
    {
        [Fact]
        public void CsdlSerializer_serializes_custom_model_namespace()
        {
            var serializedModel = new XDocument();

            using (var writer = serializedModel.CreateWriter())
            {
                new CsdlSerializer()
                    .Serialize(new EdmModel(DataSpace.CSpace), writer, "NS");
            }

            Assert.Equal("NS", (string)serializedModel.Root.Attribute("Namespace"));
        }
    }
}
