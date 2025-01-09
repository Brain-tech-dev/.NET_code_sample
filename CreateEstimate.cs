using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShutterManagement.Core.ViewModels.Request
{
    public class CreateEstimate
    {
        [Default("")]
        [Required]
        public Guid? JobId { get; set; }

        [Default("")]
    
        public Guid? taxRateId { get; set; }
      
        [Default("")]
        [Required]
        public string? EstimateName { get; set; }
        [Default("")]
        [Required]
        public Guid[]? WindowProductId { get; set; } = new Guid[] {};
        [Default("")]
        public string? TermsCondition { get; set; }
        [Default("")]
        public string? Notes { get; set; }
        [Default("")]
        public string? DiscountCoupon { get; set; }
        [Default(0.00)]
        [Required]
        public decimal? SubTotal { get; set; }
        [Default(0.00)]
        public decimal? Tax { get; set; }
        [Default(0.00)]
        public decimal? Discount { get; set; }
        [Default(0.00)]
        [Required]
        public decimal? GrandTotal { get; set; }
    }
}
