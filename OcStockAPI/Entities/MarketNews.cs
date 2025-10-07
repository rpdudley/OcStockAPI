namespace OcStockAPI.Entities
{
    [Table("MarketNews")]
    public class MarketNews
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("news_id")]
        public int NewsId { get; set; }

        [Required]
        [Column("stock_id")]
        public int StockId { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("headline")]
        public string Headline { get; set; }

        [Required]
        [MaxLength(2048)]
        [Column("source_url")]
        public string SourceUrl { get; set; }

        [Required]
        [Column("datetime")]
        public DateTime Datetime { get; set; }

        [ForeignKey("StockId")]
        public Stock Stock { get; set; }
    }
}
