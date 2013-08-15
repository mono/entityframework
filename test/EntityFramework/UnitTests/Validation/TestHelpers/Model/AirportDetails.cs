﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Validation
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    [CustomValidation(typeof(AirportDetails), "ValidateCountry")]
    public class AirportDetails : IValidatableObject
    {
        [Required]
        [RegularExpression("^[A-Z]{3}$")]
        public string AirportCode { get; set; }

        public string CityCode { get; set; }

        public string CountryCode { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            throw new NotImplementedException();
        }

        public static ValidationResult ValidateCountry(AirportDetails airportDetails, ValidationContext validationContex)
        {
            return airportDetails.CountryCode == "ZZ" && airportDetails.CityCode != "XXX"
                       ? new ValidationResult(string.Format("City '{0}' is not located in country 'ZZ'.", airportDetails.CityCode))
                       : ValidationResult.Success;
        }
    }
}