// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Model
{
    using System.Data.Entity.Resources;
    using Xunit;

    public class MoveTableOperationTests
    {
        [Fact]
        public void Ctor_should_validate_preconditions()
        {
            Assert.Equal(
                new ArgumentException(Strings.ArgumentIsNullOrWhitespace("name")).Message,
                Assert.Throws<ArgumentException>(() => new MoveTableOperation(null, null)).Message);
        }

        [Fact]
        public void Can_get_and_set_name_properties()
        {
            var moveTableOperation = new MoveTableOperation("dbo.Customers", "crm");

            Assert.Equal("dbo.Customers", moveTableOperation.Name);
            Assert.Equal("crm", moveTableOperation.NewSchema);
        }

        [Fact]
        public void Can_get_and_set_context_key_properties()
        {
            var moveTableOperation
                = new MoveTableOperation("dbo.Customers", "crm")
                      {
                          ContextKey = "foo"
                      };

            Assert.Equal("foo", moveTableOperation.ContextKey);
        }

        [Fact]
        public void Inverse_should_produce_move_table_operation()
        {
            var moveTableOperation
                = new MoveTableOperation("dbo.MyCustomers", "crm");

            var inverse = (MoveTableOperation)moveTableOperation.Inverse;

            Assert.Equal("crm.MyCustomers", inverse.Name);
            Assert.Equal("dbo", inverse.NewSchema);
        }
    }
}
