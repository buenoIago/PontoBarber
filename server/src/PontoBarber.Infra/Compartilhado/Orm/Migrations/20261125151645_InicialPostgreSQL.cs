using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

// Código gerado pelo EF Core: listas literais de colunas dos índices.
#pragma warning disable CA1861

namespace PontoBarber.Infra.Compartilhado.Orm.Migrations
{
    /// <inheritdoc />
    public partial class InicialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    UserType = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Barbearias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TelefoneComercial = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmailComercial = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Cidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Endereco = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Instagram = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CodigoConvite = table.Column<string>(type: "character(8)", fixedLength: true, maxLength: 8, nullable: false),
                    FusoHorarioId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdProfissionalProprietario = table.Column<Guid>(type: "uuid", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Barbearias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodigosOtp",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Salt = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiraEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumidoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TentativasInvalidas = table.Column<int>(type: "integer", nullable: false),
                    Reenvios = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigosOtp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExecucoesJob",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeJob = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IniciadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinalizadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Resultado = table.Column<int>(type: "integer", nullable: false),
                    QuantidadeProcessados = table.Column<int>(type: "integer", nullable: false),
                    Erro = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecucoesJob", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosIdempotencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    Chave = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HashRequisicao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    JsonResposta = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CodigoStatus = table.Column<int>(type: "integer", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiraEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosIdempotencia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profissionais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    NomeCompleto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profissionais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profissionais_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HorariosFuncionamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarbeariaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    AbreAs = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    FechaAs = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Fechado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosFuncionamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosFuncionamento_Barbearias_BarbeariaId",
                        column: x => x.BarbeariaId,
                        principalTable: "Barbearias",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Servicos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarbeariaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DuracaoMinutos = table.Column<int>(type: "integer", nullable: false),
                    Preco = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    EhModelo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicos", x => x.Id);
                    table.UniqueConstraint("AK_Servicos_BarbeariaId_Id", x => new { x.BarbeariaId, x.Id });
                    table.ForeignKey(
                        name: "FK_Servicos_Barbearias_BarbeariaId",
                        column: x => x.BarbeariaId,
                        principalTable: "Barbearias",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VinculosProfissionais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarbeariaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cargo = table.Column<int>(type: "integer", nullable: false),
                    Situacao = table.Column<int>(type: "integer", nullable: false),
                    VinculadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemovidoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinculosProfissionais", x => x.Id);
                    table.UniqueConstraint("UX_Vinculos_Barbearia_Profissional", x => new { x.BarbeariaId, x.ProfissionalId });
                    table.ForeignKey(
                        name: "FK_VinculosProfissionais_Barbearias_BarbeariaId",
                        column: x => x.BarbeariaId,
                        principalTable: "Barbearias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VinculosProfissionais_Profissionais_ProfissionalId",
                        column: x => x.ProfissionalId,
                        principalTable: "Profissionais",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Agendamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarbeariaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<string>(type: "text", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicoId = table.Column<Guid>(type: "uuid", nullable: false),
                    IniciaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TerminaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NomeServicoSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DuracaoSnapshot = table.Column<int>(type: "integer", nullable: false),
                    PrecoSnapshot = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Origem = table.Column<int>(type: "integer", nullable: false),
                    Situacao = table.Column<int>(type: "integer", nullable: false),
                    CanceladoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceladoPorUsuarioId = table.Column<string>(type: "text", nullable: true),
                    MotivoCancelamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InicioRealUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FimRealUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agendamentos", x => x.Id);
                    table.UniqueConstraint("AK_Agendamentos_BarbeariaId_Id", x => new { x.BarbeariaId, x.Id });
                    table.ForeignKey(
                        name: "FK_Agendamentos_Barbearias_BarbeariaId",
                        column: x => x.BarbeariaId,
                        principalTable: "Barbearias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Agendamentos_Servicos_BarbeariaId_ServicoId",
                        columns: x => new { x.BarbeariaId, x.ServicoId },
                        principalTable: "Servicos",
                        principalColumns: new[] { "BarbeariaId", "Id" });
                    table.ForeignKey(
                        name: "FK_Agendamentos_VinculosProfissionais_BarbeariaId_Profissional~",
                        columns: x => new { x.BarbeariaId, x.ProfissionalId },
                        principalTable: "VinculosProfissionais",
                        principalColumns: new[] { "BarbeariaId", "ProfissionalId" });
                });

            migrationBuilder.CreateTable(
                name: "BloqueiosAgenda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarbeariaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    IniciaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TerminaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BloqueiosAgenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloqueiosAgenda_Barbearias_BarbeariaId",
                        column: x => x.BarbeariaId,
                        principalTable: "Barbearias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BloqueiosAgenda_VinculosProfissionais_BarbeariaId_Profissio~",
                        columns: x => new { x.BarbeariaId, x.ProfissionalId },
                        principalTable: "VinculosProfissionais",
                        principalColumns: new[] { "BarbeariaId", "ProfissionalId" });
                });

            migrationBuilder.CreateTable(
                name: "DisponibilidadesSemanais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarbeariaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiaSemana = table.Column<int>(type: "integer", nullable: false),
                    IniciaEm = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    TerminaEm = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisponibilidadesSemanais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisponibilidadesSemanais_Barbearias_BarbeariaId",
                        column: x => x.BarbeariaId,
                        principalTable: "Barbearias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DisponibilidadesSemanais_VinculosProfissionais_BarbeariaId_~",
                        columns: x => new { x.BarbeariaId, x.ProfissionalId },
                        principalTable: "VinculosProfissionais",
                        principalColumns: new[] { "BarbeariaId", "ProfissionalId" });
                });

            migrationBuilder.CreateTable(
                name: "Financeiro",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarbeariaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgendamentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Metodo = table.Column<int>(type: "integer", nullable: false),
                    RegistradoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegistradoPorUsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Financeiro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Financeiro_Agendamentos_BarbeariaId_AgendamentoId",
                        columns: x => new { x.BarbeariaId, x.AgendamentoId },
                        principalTable: "Agendamentos",
                        principalColumns: new[] { "BarbeariaId", "Id" });
                    table.ForeignKey(
                        name: "FK_Financeiro_Barbearias_BarbeariaId",
                        column: x => x.BarbeariaId,
                        principalTable: "Barbearias",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HistoricosAgendamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarbeariaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgendamentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoEvento = table.Column<int>(type: "integer", nullable: false),
                    AtorUsuarioId = table.Column<string>(type: "text", nullable: true),
                    OcorridoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InicioAnteriorUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NovoInicioUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Detalhes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricosAgendamento", x => x.Id);
                    table.UniqueConstraint("AK_HistoricosAgendamento_BarbeariaId_Id", x => new { x.BarbeariaId, x.Id });
                    table.ForeignKey(
                        name: "FK_HistoricosAgendamento_Agendamentos_BarbeariaId_AgendamentoId",
                        columns: x => new { x.BarbeariaId, x.AgendamentoId },
                        principalTable: "Agendamentos",
                        principalColumns: new[] { "BarbeariaId", "Id" });
                    table.ForeignKey(
                        name: "FK_HistoricosAgendamento_Barbearias_BarbeariaId",
                        column: x => x.BarbeariaId,
                        principalTable: "Barbearias",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SugestoesHorario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarbeariaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgendamentoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfissionalId = table.Column<Guid>(type: "uuid", nullable: false),
                    InicioSugeridoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FimSugeridoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Situacao = table.Column<int>(type: "integer", nullable: false),
                    ExpiraEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondidoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AutorUsuarioId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SugestoesHorario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SugestoesHorario_Agendamentos_BarbeariaId_AgendamentoId",
                        columns: x => new { x.BarbeariaId, x.AgendamentoId },
                        principalTable: "Agendamentos",
                        principalColumns: new[] { "BarbeariaId", "Id" });
                    table.ForeignKey(
                        name: "FK_SugestoesHorario_Barbearias_BarbeariaId",
                        column: x => x.BarbeariaId,
                        principalTable: "Barbearias",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BarbeariaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinatarioUsuarioId = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    AgendamentoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Mensagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LidoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Agendamentos_BarbeariaId_AgendamentoId",
                        columns: x => new { x.BarbeariaId, x.AgendamentoId },
                        principalTable: "Agendamentos",
                        principalColumns: new[] { "BarbeariaId", "Id" });
                    table.ForeignKey(
                        name: "FK_Notificacoes_Barbearias_BarbeariaId",
                        column: x => x.BarbeariaId,
                        principalTable: "Barbearias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notificacoes_HistoricosAgendamento_BarbeariaId_EventId",
                        columns: x => new { x.BarbeariaId, x.EventId },
                        principalTable: "HistoricosAgendamento",
                        principalColumns: new[] { "BarbeariaId", "Id" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_BarbeariaId_ServicoId",
                table: "Agendamentos",
                columns: new[] { "BarbeariaId", "ServicoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Customer_Start",
                table: "Agendamentos",
                columns: new[] { "ClienteId", "IniciaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Appointments_Pro_Start_Active",
                table: "Agendamentos",
                columns: new[] { "BarbeariaId", "ProfissionalId", "IniciaEmUtc" },
                unique: true,
                filter: "\"Situacao\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Barbearias_CodigoConvite",
                table: "Barbearias",
                column: "CodigoConvite",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bloqueios_Barbearia_Profissional_Inicio",
                table: "BloqueiosAgenda",
                columns: new[] { "BarbeariaId", "ProfissionalId", "IniciaEmUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_CodigosOtp_Email",
                table: "CodigosOtp",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Disponibilidade_Barbearia_Profissional_Dia",
                table: "DisponibilidadesSemanais",
                columns: new[] { "BarbeariaId", "ProfissionalId", "DiaSemana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobExecutions_Name_Start",
                table: "ExecucoesJob",
                columns: new[] { "NomeJob", "IniciadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Financeiro_BarbeariaId_AgendamentoId",
                table: "Financeiro",
                columns: new[] { "BarbeariaId", "AgendamentoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Tenant_Registered",
                table: "Financeiro",
                columns: new[] { "BarbeariaId", "RegistradoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Payments_Appointment",
                table: "Financeiro",
                column: "AgendamentoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistoricosAgendamento_BarbeariaId_AgendamentoId",
                table: "HistoricosAgendamento",
                columns: new[] { "BarbeariaId", "AgendamentoId" });

            migrationBuilder.CreateIndex(
                name: "IX_History_Appointment_Occurred",
                table: "HistoricosAgendamento",
                columns: new[] { "AgendamentoId", "OcorridoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Horarios_Barbearia_Dia",
                table: "HorariosFuncionamento",
                columns: new[] { "BarbeariaId", "DiaSemana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_BarbeariaId_AgendamentoId",
                table: "Notificacoes",
                columns: new[] { "BarbeariaId", "AgendamentoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_BarbeariaId_EventId",
                table: "Notificacoes",
                columns: new[] { "BarbeariaId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Recipient_Created",
                table: "Notificacoes",
                columns: new[] { "DestinatarioUsuarioId", "CriadoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Recipient_Unread",
                table: "Notificacoes",
                columns: new[] { "DestinatarioUsuarioId", "LidoEmUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Notifications_Event_Recipient",
                table: "Notificacoes",
                columns: new[] { "EventId", "DestinatarioUsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Profissionais_UsuarioId",
                table: "Profissionais",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Idempotency_Expires",
                table: "RegistrosIdempotencia",
                column: "ExpiraEmUtc");

            migrationBuilder.CreateIndex(
                name: "UX_Idempotency_User_Key",
                table: "RegistrosIdempotencia",
                columns: new[] { "UsuarioId", "Chave" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Servicos_Barbearia_Nome",
                table: "Servicos",
                columns: new[] { "BarbeariaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SugestoesHorario_BarbeariaId_AgendamentoId",
                table: "SugestoesHorario",
                columns: new[] { "BarbeariaId", "AgendamentoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Suggestions_Status_Expires",
                table: "SugestoesHorario",
                columns: new[] { "Situacao", "ExpiraEmUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Suggestions_Appointment_Pending",
                table: "SugestoesHorario",
                columns: new[] { "AgendamentoId", "Situacao" },
                unique: true,
                filter: "\"Situacao\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_VinculosProfissionais_ProfissionalId",
                table: "VinculosProfissionais",
                column: "ProfissionalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BloqueiosAgenda");

            migrationBuilder.DropTable(
                name: "CodigosOtp");

            migrationBuilder.DropTable(
                name: "DisponibilidadesSemanais");

            migrationBuilder.DropTable(
                name: "ExecucoesJob");

            migrationBuilder.DropTable(
                name: "Financeiro");

            migrationBuilder.DropTable(
                name: "HorariosFuncionamento");

            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.DropTable(
                name: "RegistrosIdempotencia");

            migrationBuilder.DropTable(
                name: "SugestoesHorario");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "HistoricosAgendamento");

            migrationBuilder.DropTable(
                name: "Agendamentos");

            migrationBuilder.DropTable(
                name: "Servicos");

            migrationBuilder.DropTable(
                name: "VinculosProfissionais");

            migrationBuilder.DropTable(
                name: "Barbearias");

            migrationBuilder.DropTable(
                name: "Profissionais");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}

#pragma warning restore CA1861
