using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShutterManagement.Core.ViewModels.Response
{
    public class WindowProdcutDetailsForEstimate
    {// Window Properties
        public Guid WindowId { get; set; }
        public Guid? AddressId { get; set; }
        public string? Label { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public decimal? ArchHeight { get; set; }
        public decimal? Quantity { get; set; }
        public Guid? JobId { get; set; }
        public bool? IsRated { get; set; }

        // WindowProduct Properties
        public Guid? WindowProdcutId { get; set; }
        public Guid? ProductId { get; set; }
        public decimal? BuildWidth { get; set; }
        public decimal? BuildHeight { get; set; }

        // Product Properties
        public string? ProductName { get; set; }
        public decimal? ProductWidth { get; set; }
        public decimal? ProductHeight { get; set; }
        public decimal? ProductArchHeight { get; set; }
        public decimal? Area { get; set; }
        public decimal? Length { get; set; }
        public decimal? AdditionalWidth { get; set; }
        public decimal? Markup { get; set; }
        public string[]? Options { get; set; } = [];

        // ProductType Properties
        public string? ProductTypeName { get; set; }

        // ProductComponent Properties
        public Guid? ComponentId { get; set; }
        public decimal? ComponentQuantity { get; set; }

        // Component Properties
        public string? ComponentName { get; set; }
        public decimal? MarkupPercentage { get; set; }
        public decimal? Price { get; set; }

        // Expression Properties
        public string? PricingExpression { get; set; }
        public string? ExpressionFormulae { get; set; }

        // ComponentMaterial Properties
        public Guid? RawMaterialId { get; set; }
        public decimal? MaterialQuantity { get; set; }
        public decimal? WasteFactor { get; set; }

        // Material Properties
        public string? MaterialName { get; set; }
        public decimal? UnitCost { get; set; }
        public string? UnitOfMeasurement { get; set; }
        public decimal? StockQuantity { get; set; }
        public decimal? ReorderLevel { get; set; }
        public bool? IsCutSheet { get; set; }

        //Total price
        public decimal? TotalPrice { get; set; }

    }
}
