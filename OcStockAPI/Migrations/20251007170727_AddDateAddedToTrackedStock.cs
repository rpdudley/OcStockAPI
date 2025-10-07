using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OcStockAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddDateAddedToTrackedStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiCallLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CallDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CallType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiCallLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    federal_interest_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    unemployment_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    inflation = table.Column<decimal>(type: "numeric", nullable: true),
                    cpi = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "InvestorAccount",
                columns: table => new
                {
                    account_ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestorAccount", x => x.account_ID);
                });

            migrationBuilder.CreateTable(
                name: "MutualFunds",
                columns: table => new
                {
                    MutualFundId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutualFunds", x => x.MutualFundId);
                });

            migrationBuilder.CreateTable(
                name: "TrackedStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    StockName = table.Column<string>(type: "text", nullable: true),
                    date_added = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedStocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Portfolio",
                columns: table => new
                {
                    PortfolioId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolio", x => x.PortfolioId);
                    table.ForeignKey(
                        name: "FK_Portfolio_InvestorAccount_AccountId",
                        column: x => x.AccountId,
                        principalTable: "InvestorAccount",
                        principalColumn: "account_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Event_MutualFunds",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    MutualFundId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event_MutualFunds", x => new { x.EventId, x.MutualFundId });
                    table.ForeignKey(
                        name: "FK_Event_MutualFunds_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Event_MutualFunds_MutualFunds_MutualFundId",
                        column: x => x.MutualFundId,
                        principalTable: "MutualFunds",
                        principalColumn: "MutualFundId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    stock_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Symbol = table.Column<string>(type: "text", nullable: true),
                    OpenValue = table.Column<decimal>(type: "numeric", nullable: false),
                    ClosingValue = table.Column<decimal>(type: "numeric", nullable: false),
                    tracked_stock_id = table.Column<int>(type: "integer", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.stock_id);
                    table.ForeignKey(
                        name: "FK_Stocks_TrackedStocks_tracked_stock_id",
                        column: x => x.tracked_stock_id,
                        principalTable: "TrackedStocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Portfolio_MutualFunds",
                columns: table => new
                {
                    PortfolioId = table.Column<int>(type: "integer", nullable: false),
                    MutualFundId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolio_MutualFunds", x => new { x.PortfolioId, x.MutualFundId });
                    table.ForeignKey(
                        name: "FK_Portfolio_MutualFunds_MutualFunds_MutualFundId",
                        column: x => x.MutualFundId,
                        principalTable: "MutualFunds",
                        principalColumn: "MutualFundId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Portfolio_MutualFunds_Portfolio_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolio",
                        principalColumn: "PortfolioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Event_Stocks",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    stock_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event_Stocks", x => new { x.event_id, x.stock_id });
                    table.ForeignKey(
                        name: "FK_Event_Stocks_Events_event_id",
                        column: x => x.event_id,
                        principalTable: "Events",
                        principalColumn: "event_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Event_Stocks_Stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "Stocks",
                        principalColumn: "stock_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketNews",
                columns: table => new
                {
                    news_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_id = table.Column<int>(type: "integer", nullable: false),
                    headline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    source_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketNews", x => x.news_id);
                    table.ForeignKey(
                        name: "FK_MarketNews_Stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "Stocks",
                        principalColumn: "stock_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Portfolio_Stocks",
                columns: table => new
                {
                    PortfolioId = table.Column<int>(type: "integer", nullable: false),
                    StockId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolio_Stocks", x => new { x.PortfolioId, x.StockId });
                    table.ForeignKey(
                        name: "FK_Portfolio_Stocks_Portfolio_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolio",
                        principalColumn: "PortfolioId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Portfolio_Stocks_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "stock_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockHistory",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stock_id = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpenedValue = table.Column<decimal>(type: "numeric", nullable: true),
                    ClosedValue = table.Column<decimal>(type: "numeric", nullable: true),
                    Volume = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockHistory", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_StockHistory_Stocks_stock_id",
                        column: x => x.stock_id,
                        principalTable: "Stocks",
                        principalColumn: "stock_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Event_MutualFunds_MutualFundId",
                table: "Event_MutualFunds",
                column: "MutualFundId");

            migrationBuilder.CreateIndex(
                name: "IX_Event_Stocks_stock_id",
                table: "Event_Stocks",
                column: "stock_id");

            migrationBuilder.CreateIndex(
                name: "IX_MarketNews_stock_id",
                table: "MarketNews",
                column: "stock_id");

            migrationBuilder.CreateIndex(
                name: "IX_Portfolio_AccountId",
                table: "Portfolio",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Portfolio_MutualFunds_MutualFundId",
                table: "Portfolio_MutualFunds",
                column: "MutualFundId");

            migrationBuilder.CreateIndex(
                name: "IX_Portfolio_Stocks_StockId",
                table: "Portfolio_Stocks",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_StockHistory_stock_id",
                table: "StockHistory",
                column: "stock_id");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_tracked_stock_id",
                table: "Stocks",
                column: "tracked_stock_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiCallLog");

            migrationBuilder.DropTable(
                name: "Event_MutualFunds");

            migrationBuilder.DropTable(
                name: "Event_Stocks");

            migrationBuilder.DropTable(
                name: "MarketNews");

            migrationBuilder.DropTable(
                name: "Portfolio_MutualFunds");

            migrationBuilder.DropTable(
                name: "Portfolio_Stocks");

            migrationBuilder.DropTable(
                name: "StockHistory");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "MutualFunds");

            migrationBuilder.DropTable(
                name: "Portfolio");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "InvestorAccount");

            migrationBuilder.DropTable(
                name: "TrackedStocks");
        }
    }
}
