using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BackendMoviles.Infrastructure.Persistence.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyTips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Body = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FromWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    ToWeek = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyTips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FamilyGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HealthTelemetry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MotherUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FetalHeartRate = table.Column<int>(type: "INTEGER", nullable: false),
                    Temperature = table.Column<decimal>(type: "TEXT", nullable: false),
                    MeasuredAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthTelemetry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PregnancyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MotherUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastMenstrualPeriod = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PregnancyRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TriageConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MotherUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    LatestAssessment_Urgency = table.Column<int>(type: "INTEGER", nullable: true),
                    LatestAssessment_Guidance = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    LatestAssessment_AssessedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriageConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TipCompletions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TipId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PartnerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TipCompletions_DailyTips_TipId",
                        column: x => x.TipId,
                        principalTable: "DailyTips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FamilyGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ExpiresAt = table.Column<long>(type: "INTEGER", nullable: false),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UsedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyInvitations_FamilyGroups_FamilyGroupId",
                        column: x => x.FamilyGroupId,
                        principalTable: "FamilyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FamilyMember",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    FamilyGroupId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FamilyMember_FamilyGroups_FamilyGroupId",
                        column: x => x.FamilyGroupId,
                        principalTable: "FamilyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuickAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FamilyGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Response = table.Column<int>(type: "INTEGER", nullable: true),
                    RespondedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RespondedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuickAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuickAlerts_FamilyGroups_FamilyGroupId",
                        column: x => x.FamilyGroupId,
                        principalTable: "FamilyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SymptomEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Severity = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurredAt = table.Column<long>(type: "INTEGER", nullable: false),
                    PregnancyRecordId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymptomEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SymptomEntry_PregnancyRecords_PregnancyRecordId",
                        column: x => x.PregnancyRecordId,
                        principalTable: "PregnancyRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TriageMessage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Author = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    SentAt = table.Column<long>(type: "INTEGER", nullable: false),
                    TriageConversationId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriageMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TriageMessage_TriageConversations_TriageConversationId",
                        column: x => x.TriageConversationId,
                        principalTable: "TriageConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DailyTips",
                columns: new[] { "Id", "Body", "FromWeek", "Title", "ToWeek" },
                values: new object[,]
                {
                    { new Guid("d1a7e000-0000-4000-8000-000000000001"), "Ten siempre a mano una botella de agua para ella. Una buena hidratación ayuda con el cansancio, el estreñimiento y la hinchazón.", 1, "Recuérdale hidratarse", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000002"), "Dedica unos minutos al día a escucharla sin juzgar ni dar soluciones rápidas. Sentirse acompañada también es cuidado.", 1, "Pregúntale cómo se siente", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000003"), "Asume tareas como lavar, limpiar o hacer las compras sin esperar a que te lo pida. Su energía cambia mucho durante el embarazo.", 1, "Reparte las tareas de la casa", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000004"), "Evita fumar cerca de ella y en casa, y pide lo mismo a las visitas. El humo de segunda mano también afecta al bebé.", 1, "Mantén el humo lejos", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000005"), "En las primeras semanas el ácido fólico es clave. Ayúdala a recordar la toma diaria que indicó su equipo de salud.", 1, "Recuérdale sus vitaminas", 13 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000006"), "El primer trimestre suele traer mucho sueño. Facilítale siestas y descanso sin hacerla sentir culpable.", 1, "Respeta su cansancio", 14 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000007"), "La primera consulta prenatal trae mucha información. Ir juntos te ayuda a entender el control y a anotar las dudas.", 4, "Acompáñala a la primera consulta", 12 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000008"), "Galletas de agua, fruta o comidas pequeñas y frecuentes pueden aliviar las náuseas. Pregúntale qué le cae mejor.", 4, "Ten a mano algo para las náuseas", 14 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000009"), "Muchas gestantes se vuelven sensibles a los olores. Evita perfumes intensos y ventila la cocina al preparar comida.", 5, "Cuida los olores fuertes", 14 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000010"), "Ver y escuchar al bebé por primera vez es un momento especial. Organiza tu horario para estar presente.", 8, "Acompáñala a la primera ecografía", 13 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000011"), "Desde el segundo trimestre conviene que no cargue bolsas pesadas ni mueva muebles. Ofrécete antes de que lo intente.", 14, "Evita que cargue peso o paquetes", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000012"), "Una caminata suave y diaria, si su equipo de salud lo aprueba, mejora la circulación y el ánimo de los dos.", 14, "Salgan a caminar juntos", 34 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000013"), "La zona lumbar suele cargarse a medida que crece la barriga. Un masaje suave con movimientos circulares alivia mucho.", 16, "Dale un masaje lumbar", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000014"), "Entre las semanas 18 y 22 se revisa en detalle el desarrollo del bebé. Pide el día libre para ir con ella.", 18, "Acompáñala a la ecografía morfológica", 22 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000015"), "Pon la mano sobre su barriga cuando el bebé esté activo y háblale. El bebé empieza a reconocer voces.", 18, "Siente las pataditas", 30 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000016"), "Busquen juntos un curso de preparación para el parto. Aprenderás cómo apoyarla en el trabajo de parto.", 20, "Inscríbanse al curso de preparación", 30 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000017"), "Sangrado, dolor intenso, fiebre, visión borrosa o que el bebé se mueva menos son motivos para ir a urgencias. Tenlos claros.", 20, "Aprende las señales de alarma", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000018"), "Arma la cuna, organiza la ropa o pinta la habitación. Hacerlo juntos con tiempo evita apuros al final.", 20, "Preparen el espacio del bebé", 32 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000019"), "Agacharse se vuelve difícil. Ayúdala a ponerse zapatos y medias, y busquen calzado cómodo y sin tacón.", 24, "Ayúdala con los zapatos", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000020"), "Si se le hinchan los pies, ofrécele un cojín para elevar las piernas al final del día. Si la hinchazón es brusca, consulten.", 24, "Eleva sus piernas al descansar", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000021"), "Entre las semanas 24 y 28 suele hacerse la prueba de tolerancia a la glucosa, que implica ayuno y espera. Acompáñala.", 24, "Acompáñala a la prueba de glucosa", 28 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000022"), "Averigua los requisitos de admisión, el seguro y los documentos que pide la clínica. Déjalos listos en una carpeta.", 28, "Adelanta los trámites de la clínica", 36 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000023"), "Dormir de lado es más cómodo y recomendable en el tercer trimestre. Consigue almohadas extra para su espalda y piernas.", 28, "Ayúdala a dormir de lado", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000024"), "Pregúntale cada día si sintió al bebé moverse como siempre. Si nota menos movimientos, llamen a su equipo de salud.", 28, "Atentos a los movimientos del bebé", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000025"), "Habla en tu trabajo sobre la licencia por paternidad y deja tus pendientes organizados para poder ausentarte.", 28, "Coordina tu licencia", 38 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000026"), "Arma con ella el bolso con ropa cómoda, artículos de aseo, ropa del bebé, pañales y sus documentos. Déjalo junto a la puerta.", 30, "Prepara el bolso para la clínica", 36 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000027"), "Define cómo llegarán a la clínica de día y de noche, cuánto tiempo toma y una alternativa si no hay transporte.", 32, "Planifica la ruta a la clínica", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000028"), "Si viajarán en auto al salir de la clínica, instala con tiempo una silla adecuada para recién nacidos.", 32, "Consigue la silla para el auto", 40 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000029"), "Anota cuándo empieza cada contracción y cuánto dura. Su equipo de salud le dirá con qué frecuencia deben ir a la clínica.", 34, "Aprende a cronometrar contracciones", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000030"), "Prepara y congela algunas comidas para las primeras semanas con el bebé. Será un alivio para los dos.", 34, "Cocina para el posparto", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000031"), "En las últimas semanas los controles son más frecuentes. Ve con ella y pregunta cómo identificar el inicio del trabajo de parto.", 36, "Acompáñala a los controles finales", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000032"), "Desde ahora ten el celular con batería y el volumen activo. Ella debe poder ubicarte en cualquier momento.", 36, "Mantén el teléfono cargado", 42 },
                    { new Guid("d1a7e000-0000-4000-8000-000000000033"), "Muchos bebés nacen después de la fecha probable de parto. Ayúdala a distraerse y a seguir las indicaciones de su equipo.", 39, "Paciencia si pasa la fecha probable", 42 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyInvitations_Code",
                table: "FamilyInvitations",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyInvitations_FamilyGroupId",
                table: "FamilyInvitations",
                column: "FamilyGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMember_FamilyGroupId_UserId",
                table: "FamilyMember",
                columns: new[] { "FamilyGroupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthTelemetry_MotherUserId_MeasuredAt",
                table: "HealthTelemetry",
                columns: new[] { "MotherUserId", "MeasuredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PregnancyRecords_MotherUserId",
                table: "PregnancyRecords",
                column: "MotherUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuickAlerts_FamilyGroupId_CreatedAt",
                table: "QuickAlerts",
                columns: new[] { "FamilyGroupId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SymptomEntry_PregnancyRecordId",
                table: "SymptomEntry",
                column: "PregnancyRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TipCompletions_TipId_PartnerUserId_Date",
                table: "TipCompletions",
                columns: new[] { "TipId", "PartnerUserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TriageConversations_MotherUserId",
                table: "TriageConversations",
                column: "MotherUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TriageMessage_TriageConversationId",
                table: "TriageMessage",
                column: "TriageConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamilyInvitations");

            migrationBuilder.DropTable(
                name: "FamilyMember");

            migrationBuilder.DropTable(
                name: "HealthTelemetry");

            migrationBuilder.DropTable(
                name: "QuickAlerts");

            migrationBuilder.DropTable(
                name: "SymptomEntry");

            migrationBuilder.DropTable(
                name: "TipCompletions");

            migrationBuilder.DropTable(
                name: "TriageMessage");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "FamilyGroups");

            migrationBuilder.DropTable(
                name: "PregnancyRecords");

            migrationBuilder.DropTable(
                name: "DailyTips");

            migrationBuilder.DropTable(
                name: "TriageConversations");
        }
    }
}
