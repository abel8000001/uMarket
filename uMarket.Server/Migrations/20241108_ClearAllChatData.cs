using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uMarket.Server.Migrations
{
    /// <inheritdoc />
    public partial class ClearAllChatData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete all messages first (foreign key constraint)
            migrationBuilder.Sql("DELETE FROM Messages;");
            
            // Delete all conversation participants
            migrationBuilder.Sql("DELETE FROM ConversationParticipants;");
            
            // Delete all conversations
            migrationBuilder.Sql("DELETE FROM Conversations;");
            
            // Delete all chat requests
            migrationBuilder.Sql("DELETE FROM ChatRequests;");
            
            // Reset identity seeds if needed
            migrationBuilder.Sql("DBCC CHECKIDENT ('Messages', RESEED, 0);");
            migrationBuilder.Sql("DBCC CHECKIDENT ('Conversations', RESEED, 0);");
            migrationBuilder.Sql("DBCC CHECKIDENT ('ChatRequests', RESEED, 0);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback for data deletion
            // This is a one-way operation
        }
    }
}
