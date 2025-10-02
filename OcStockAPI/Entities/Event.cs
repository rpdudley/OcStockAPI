using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OcStockAPI.Entities
{
    [Table("Events")]
    public class Event
    {
        [Key]
        [Column("event_id")]
        public int EventId { get; set; }

        [Column("datetime")]
        public DateTime? Datetime { get; set; }

        [Column("federal_interest_rate")]
        public decimal? FederalInterestRate { get; set; }

        [Column("unemployment_rate")]
        public decimal? UnemploymentRate { get; set; }

        [Column("inflation")]
        public decimal? Inflation { get; set; }

        [Column("cpi")]
        public decimal? CPI { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public ICollection<EventStock> EventStocks { get; set; }
    }
}
