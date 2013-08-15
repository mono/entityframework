﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.TestModels.GearsOfWarModel
{
    using System.ComponentModel.DataAnnotations;
    using System.Data.Entity.Spatial;

    public class City
    {
        // non-integer key with not conventional name
        public string Name { get; set; }
        public DbGeography Location { get; set; }
    }
}
