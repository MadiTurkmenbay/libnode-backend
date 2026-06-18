using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibNode.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Триграммный поиск (pg_trgm) — ускоряет ILIKE '%...%' по названию/описанию каталога.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_books_title_trgm ON \"Books\" USING gin (\"Title\" gin_trgm_ops);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_books_description_trgm ON \"Books\" USING gin (\"Description\" gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_books_description_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_books_title_trgm;");
            // Расширение pg_trgm намеренно не удаляем (может использоваться другими объектами).
        }
    }
}
