using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timeblock.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialM4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ASIGNATURA",
                columns: table => new
                {
                    idAsignatura = table.Column<Guid>(type: "uuid", nullable: false),
                    nombreAsignatura = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASIGNATURA", x => x.idAsignatura);
                });

            migrationBuilder.CreateTable(
                name: "AULA",
                columns: table => new
                {
                    idAula = table.Column<Guid>(type: "uuid", nullable: false),
                    edificio = table.Column<string>(type: "text", nullable: false),
                    capacidad = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AULA", x => x.idAula);
                    table.CheckConstraint("CK_AULA_CapacidadPositiva", "\"capacidad\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "FECHA",
                columns: table => new
                {
                    fecha = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FECHA", x => x.fecha);
                });

            migrationBuilder.CreateTable(
                name: "PERSONA",
                columns: table => new
                {
                    idPersona = table.Column<Guid>(type: "uuid", nullable: false),
                    nombrePersona = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PERSONA", x => x.idPersona);
                });

            migrationBuilder.CreateTable(
                name: "PROGRAMA_ACADEMICO",
                columns: table => new
                {
                    idPrograma = table.Column<Guid>(type: "uuid", nullable: false),
                    nombrePrograma = table.Column<string>(type: "text", nullable: false),
                    planEstudio = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROGRAMA_ACADEMICO", x => x.idPrograma);
                });

            migrationBuilder.CreateTable(
                name: "COORDINADOR",
                columns: table => new
                {
                    idCoordinador = table.Column<Guid>(type: "uuid", nullable: false),
                    especialidad = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COORDINADOR", x => x.idCoordinador);
                    table.ForeignKey(
                        name: "FK_COORDINADOR_PERSONA_idCoordinador",
                        column: x => x.idCoordinador,
                        principalTable: "PERSONA",
                        principalColumn: "idPersona",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JEFE_DEPTO",
                columns: table => new
                {
                    idJefeDepto = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JEFE_DEPTO", x => x.idJefeDepto);
                    table.ForeignKey(
                        name: "FK_JEFE_DEPTO_PERSONA_idJefeDepto",
                        column: x => x.idJefeDepto,
                        principalTable: "PERSONA",
                        principalColumn: "idPersona",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PROGRAMA_ASIGNATURA",
                columns: table => new
                {
                    idPrograma = table.Column<Guid>(type: "uuid", nullable: false),
                    idAsignatura = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROGRAMA_ASIGNATURA", x => new { x.idPrograma, x.idAsignatura });
                    table.ForeignKey(
                        name: "FK_PROGRAMA_ASIGNATURA_ASIGNATURA_idAsignatura",
                        column: x => x.idAsignatura,
                        principalTable: "ASIGNATURA",
                        principalColumn: "idAsignatura",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PROGRAMA_ASIGNATURA_PROGRAMA_ACADEMICO_idPrograma",
                        column: x => x.idPrograma,
                        principalTable: "PROGRAMA_ACADEMICO",
                        principalColumn: "idPrograma",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BLOQUE_CONTENIDO",
                columns: table => new
                {
                    idBloque = table.Column<Guid>(type: "uuid", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    descripcionNormalizada = table.Column<string>(type: "text", nullable: false),
                    tipoActividad = table.Column<string>(type: "text", nullable: false),
                    nivelComplejidad = table.Column<string>(type: "text", nullable: false),
                    idAsignatura = table.Column<Guid>(type: "uuid", nullable: false),
                    idCoordinadorCreador = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BLOQUE_CONTENIDO", x => x.idBloque);
                    table.CheckConstraint("CK_BLOQUE_CONTENIDO_DescripcionNormalizada", "\"descripcionNormalizada\" = lower(regexp_replace(btrim(\"descripcion\"), '[[:space:]]+', ' ', 'g'))");
                    table.CheckConstraint("CK_BLOQUE_CONTENIDO_NivelComplejidad", "\"nivelComplejidad\" IN ('BAJO', 'MEDIO', 'ALTO')");
                    table.CheckConstraint("CK_BLOQUE_CONTENIDO_TipoActividad", "\"tipoActividad\" IN ('TEORICA', 'PRACTICA', 'LABORATORIO')");
                    table.ForeignKey(
                        name: "FK_BLOQUE_CONTENIDO_ASIGNATURA_idAsignatura",
                        column: x => x.idAsignatura,
                        principalTable: "ASIGNATURA",
                        principalColumn: "idAsignatura",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BLOQUE_CONTENIDO_COORDINADOR_idCoordinadorCreador",
                        column: x => x.idCoordinadorCreador,
                        principalTable: "COORDINADOR",
                        principalColumn: "idCoordinador",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "COORDINADOR_EN_PROGRAMA",
                columns: table => new
                {
                    idCoordinador = table.Column<Guid>(type: "uuid", nullable: false),
                    idPrograma = table.Column<Guid>(type: "uuid", nullable: false),
                    fechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fechaFin = table.Column<DateOnly>(type: "date", nullable: true),
                    puedeGenerar = table.Column<bool>(type: "boolean", nullable: false),
                    puedeValidar = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COORDINADOR_EN_PROGRAMA", x => new { x.idCoordinador, x.idPrograma, x.fechaInicio });
                    table.CheckConstraint("CK_COORDINADOR_EN_PROGRAMA_Vigencia", "\"fechaFin\" IS NULL OR \"fechaFin\" >= \"fechaInicio\"");
                    table.ForeignKey(
                        name: "FK_COORDINADOR_EN_PROGRAMA_COORDINADOR_idCoordinador",
                        column: x => x.idCoordinador,
                        principalTable: "COORDINADOR",
                        principalColumn: "idCoordinador",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_COORDINADOR_EN_PROGRAMA_FECHA_fechaFin",
                        column: x => x.fechaFin,
                        principalTable: "FECHA",
                        principalColumn: "fecha",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_COORDINADOR_EN_PROGRAMA_FECHA_fechaInicio",
                        column: x => x.fechaInicio,
                        principalTable: "FECHA",
                        principalColumn: "fecha",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_COORDINADOR_EN_PROGRAMA_PROGRAMA_ACADEMICO_idPrograma",
                        column: x => x.idPrograma,
                        principalTable: "PROGRAMA_ACADEMICO",
                        principalColumn: "idPrograma",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ESTUDIANTE",
                columns: table => new
                {
                    idEstudiante = table.Column<Guid>(type: "uuid", nullable: false),
                    edad = table.Column<int>(type: "integer", nullable: false),
                    grupo = table.Column<string>(type: "text", nullable: false),
                    idCoordinadorAsig = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ESTUDIANTE", x => x.idEstudiante);
                    table.CheckConstraint("CK_ESTUDIANTE_EdadPositiva", "\"edad\" > 0");
                    table.ForeignKey(
                        name: "FK_ESTUDIANTE_COORDINADOR_idCoordinadorAsig",
                        column: x => x.idCoordinadorAsig,
                        principalTable: "COORDINADOR",
                        principalColumn: "idCoordinador",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ESTUDIANTE_PERSONA_idEstudiante",
                        column: x => x.idEstudiante,
                        principalTable: "PERSONA",
                        principalColumn: "idPersona",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BLOQUE_PROGRAMA",
                columns: table => new
                {
                    idBloque = table.Column<Guid>(type: "uuid", nullable: false),
                    idPrograma = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BLOQUE_PROGRAMA", x => new { x.idBloque, x.idPrograma });
                    table.ForeignKey(
                        name: "FK_BLOQUE_PROGRAMA_BLOQUE_CONTENIDO_idBloque",
                        column: x => x.idBloque,
                        principalTable: "BLOQUE_CONTENIDO",
                        principalColumn: "idBloque",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BLOQUE_PROGRAMA_PROGRAMA_ACADEMICO_idPrograma",
                        column: x => x.idPrograma,
                        principalTable: "PROGRAMA_ACADEMICO",
                        principalColumn: "idPrograma",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ESQUEMA_LLAVE",
                columns: table => new
                {
                    idPersona = table.Column<Guid>(type: "uuid", nullable: false),
                    idCoordinador = table.Column<Guid>(type: "uuid", nullable: false),
                    fechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    fechaAsignacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    idBloque = table.Column<Guid>(type: "uuid", nullable: false),
                    idValidacion = table.Column<Guid>(type: "uuid", nullable: false),
                    idRevision = table.Column<Guid>(type: "uuid", nullable: false),
                    idSolicitud = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ESQUEMA_LLAVE", x => new { x.idPersona, x.idCoordinador, x.fechaInicio, x.fecha, x.fechaAsignacion, x.idBloque, x.idValidacion, x.idRevision, x.idSolicitud });
                    table.ForeignKey(
                        name: "FK_ESQUEMA_LLAVE_BLOQUE_CONTENIDO_idBloque",
                        column: x => x.idBloque,
                        principalTable: "BLOQUE_CONTENIDO",
                        principalColumn: "idBloque",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ESQUEMA_LLAVE_COORDINADOR_idCoordinador",
                        column: x => x.idCoordinador,
                        principalTable: "COORDINADOR",
                        principalColumn: "idCoordinador",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ESQUEMA_LLAVE_FECHA_fecha",
                        column: x => x.fecha,
                        principalTable: "FECHA",
                        principalColumn: "fecha",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ESQUEMA_LLAVE_FECHA_fechaInicio",
                        column: x => x.fechaInicio,
                        principalTable: "FECHA",
                        principalColumn: "fecha",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ESQUEMA_LLAVE_PERSONA_idPersona",
                        column: x => x.idPersona,
                        principalTable: "PERSONA",
                        principalColumn: "idPersona",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HORARIO",
                columns: table => new
                {
                    idHorario = table.Column<Guid>(type: "uuid", nullable: false),
                    fechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estadoHorario = table.Column<string>(type: "text", nullable: false),
                    periodoAcademico = table.Column<string>(type: "text", nullable: false),
                    idPrograma = table.Column<Guid>(type: "uuid", nullable: false),
                    idCoordinadorGen = table.Column<Guid>(type: "uuid", nullable: false),
                    fechaInicioGen = table.Column<DateOnly>(type: "date", nullable: false),
                    coberturaAsignatura = table.Column<decimal>(type: "numeric", nullable: true),
                    idRevisionOrigen = table.Column<Guid>(type: "uuid", nullable: true),
                    nivelComplejidadHorario = table.Column<string>(type: "text", nullable: true),
                    propLaboratorio = table.Column<decimal>(type: "numeric", nullable: true),
                    propPractica = table.Column<decimal>(type: "numeric", nullable: true),
                    propTeorica = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HORARIO", x => x.idHorario);
                    table.CheckConstraint("CK_HORARIO_CoberturaNoNegativa", "\"coberturaAsignatura\" IS NULL OR \"coberturaAsignatura\" >= 0");
                    table.CheckConstraint("CK_HORARIO_Estado", "\"estadoHorario\" IN ('GENERADO', 'EN_REVISION', 'APROBADO', 'REQUIERE_REGENERACION', 'PENDIENTE_VALIDACION', 'VALIDADO', 'RECHAZADO', 'CANCELADO')");
                    table.CheckConstraint("CK_HORARIO_Proporciones", "(\"propTeorica\" IS NULL AND \"propPractica\" IS NULL AND \"propLaboratorio\" IS NULL) OR (\"propTeorica\" IS NOT NULL AND \"propPractica\" IS NOT NULL AND \"propLaboratorio\" IS NOT NULL AND \"propTeorica\" BETWEEN 0 AND 1 AND \"propPractica\" BETWEEN 0 AND 1 AND \"propLaboratorio\" BETWEEN 0 AND 1 AND \"propTeorica\" + \"propPractica\" + \"propLaboratorio\" = 1)");
                    table.ForeignKey(
                        name: "FK_HORARIO_COORDINADOR_EN_PROGRAMA_idCoordinadorGen_idPrograma~",
                        columns: x => new { x.idCoordinadorGen, x.idPrograma, x.fechaInicioGen },
                        principalTable: "COORDINADOR_EN_PROGRAMA",
                        principalColumns: new[] { "idCoordinador", "idPrograma", "fechaInicio" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HORARIO_PROGRAMA_ACADEMICO_idPrograma",
                        column: x => x.idPrograma,
                        principalTable: "PROGRAMA_ACADEMICO",
                        principalColumn: "idPrograma",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MATRICULA",
                columns: table => new
                {
                    idEstudiante = table.Column<Guid>(type: "uuid", nullable: false),
                    idPrograma = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MATRICULA", x => new { x.idEstudiante, x.idPrograma });
                    table.ForeignKey(
                        name: "FK_MATRICULA_ESTUDIANTE_idEstudiante",
                        column: x => x.idEstudiante,
                        principalTable: "ESTUDIANTE",
                        principalColumn: "idEstudiante",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MATRICULA_PROGRAMA_ACADEMICO_idPrograma",
                        column: x => x.idPrograma,
                        principalTable: "PROGRAMA_ACADEMICO",
                        principalColumn: "idPrograma",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BLOQUE_EN_HORARIO",
                columns: table => new
                {
                    idHorario = table.Column<Guid>(type: "uuid", nullable: false),
                    idBloque = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    horaInicio = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    horaFin = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    periodoImp = table.Column<string>(type: "text", nullable: true),
                    idAula = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BLOQUE_EN_HORARIO", x => new { x.idHorario, x.idBloque, x.fecha });
                    table.CheckConstraint("CK_BLOQUE_EN_HORARIO_ImparticionCompleta", "(\"horaInicio\" IS NULL AND \"horaFin\" IS NULL AND \"periodoImp\" IS NULL AND \"idAula\" IS NULL) OR (\"horaInicio\" IS NOT NULL AND \"horaFin\" IS NOT NULL AND \"periodoImp\" IS NOT NULL AND btrim(\"periodoImp\") <> '' AND \"idAula\" IS NOT NULL AND \"horaInicio\" < \"horaFin\")");
                    table.ForeignKey(
                        name: "FK_BLOQUE_EN_HORARIO_AULA_idAula",
                        column: x => x.idAula,
                        principalTable: "AULA",
                        principalColumn: "idAula",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BLOQUE_EN_HORARIO_BLOQUE_CONTENIDO_idBloque",
                        column: x => x.idBloque,
                        principalTable: "BLOQUE_CONTENIDO",
                        principalColumn: "idBloque",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BLOQUE_EN_HORARIO_FECHA_fecha",
                        column: x => x.fecha,
                        principalTable: "FECHA",
                        principalColumn: "fecha",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BLOQUE_EN_HORARIO_HORARIO_idHorario",
                        column: x => x.idHorario,
                        principalTable: "HORARIO",
                        principalColumn: "idHorario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BLOQUE_CONTENIDO_idCoordinadorCreador",
                table: "BLOQUE_CONTENIDO",
                column: "idCoordinadorCreador");

            migrationBuilder.CreateIndex(
                name: "UX_BLOQUE_CONTENIDO_ClaveDuplicidad",
                table: "BLOQUE_CONTENIDO",
                columns: new[] { "idAsignatura", "tipoActividad", "descripcionNormalizada" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BLOQUE_EN_HORARIO_fecha",
                table: "BLOQUE_EN_HORARIO",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "IX_BLOQUE_EN_HORARIO_idBloque",
                table: "BLOQUE_EN_HORARIO",
                column: "idBloque");

            migrationBuilder.CreateIndex(
                name: "UX_BLOQUE_EN_HORARIO_AulaFechaHoraInicio",
                table: "BLOQUE_EN_HORARIO",
                columns: new[] { "idAula", "fecha", "horaInicio" },
                unique: true,
                filter: "\"idAula\" IS NOT NULL AND \"horaInicio\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BLOQUE_PROGRAMA_idPrograma",
                table: "BLOQUE_PROGRAMA",
                column: "idPrograma");

            migrationBuilder.CreateIndex(
                name: "IX_COORDINADOR_EN_PROGRAMA_fechaFin",
                table: "COORDINADOR_EN_PROGRAMA",
                column: "fechaFin");

            migrationBuilder.CreateIndex(
                name: "IX_COORDINADOR_EN_PROGRAMA_fechaInicio",
                table: "COORDINADOR_EN_PROGRAMA",
                column: "fechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_COORDINADOR_EN_PROGRAMA_idPrograma",
                table: "COORDINADOR_EN_PROGRAMA",
                column: "idPrograma");

            migrationBuilder.CreateIndex(
                name: "IX_ESQUEMA_LLAVE_fecha",
                table: "ESQUEMA_LLAVE",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "IX_ESQUEMA_LLAVE_fechaInicio",
                table: "ESQUEMA_LLAVE",
                column: "fechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_ESQUEMA_LLAVE_idBloque",
                table: "ESQUEMA_LLAVE",
                column: "idBloque");

            migrationBuilder.CreateIndex(
                name: "IX_ESQUEMA_LLAVE_idCoordinador",
                table: "ESQUEMA_LLAVE",
                column: "idCoordinador");

            migrationBuilder.CreateIndex(
                name: "IX_ESTUDIANTE_idCoordinadorAsig",
                table: "ESTUDIANTE",
                column: "idCoordinadorAsig");

            migrationBuilder.CreateIndex(
                name: "IX_HORARIO_idCoordinadorGen_idPrograma_fechaInicioGen",
                table: "HORARIO",
                columns: new[] { "idCoordinadorGen", "idPrograma", "fechaInicioGen" });

            migrationBuilder.CreateIndex(
                name: "IX_HORARIO_idPrograma",
                table: "HORARIO",
                column: "idPrograma");

            migrationBuilder.CreateIndex(
                name: "IX_MATRICULA_idPrograma",
                table: "MATRICULA",
                column: "idPrograma");

            migrationBuilder.CreateIndex(
                name: "IX_PROGRAMA_ASIGNATURA_idAsignatura",
                table: "PROGRAMA_ASIGNATURA",
                column: "idAsignatura");

            // RII03: la asignatura del bloque debe pertenecer al programa.
            // Se protege también frente a cambios directos en las tres relaciones.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION fn_m4_validar_rii03() RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_TABLE_NAME = 'BLOQUE_PROGRAMA' THEN
                        IF NOT EXISTS (
                            SELECT 1
                            FROM "BLOQUE_CONTENIDO" AS b
                            JOIN "PROGRAMA_ASIGNATURA" AS pa
                              ON pa."idAsignatura" = b."idAsignatura"
                            WHERE b."idBloque" = NEW."idBloque"
                              AND pa."idPrograma" = NEW."idPrograma"
                        ) THEN
                            RAISE EXCEPTION 'RII03: la asignatura del bloque debe pertenecer al programa.'
                                USING ERRCODE = '23514';
                        END IF;
                    ELSIF TG_TABLE_NAME = 'BLOQUE_CONTENIDO' THEN
                        IF NEW."idAsignatura" IS DISTINCT FROM OLD."idAsignatura"
                           AND EXISTS (
                               SELECT 1
                               FROM "BLOQUE_PROGRAMA" AS bp
                               WHERE bp."idBloque" = OLD."idBloque"
                                 AND NOT EXISTS (
                                     SELECT 1
                                     FROM "PROGRAMA_ASIGNATURA" AS pa
                                     WHERE pa."idPrograma" = bp."idPrograma"
                                       AND pa."idAsignatura" = NEW."idAsignatura"
                                 )
                           ) THEN
                            RAISE EXCEPTION 'RII03: no se puede cambiar la asignatura mientras el bloque esté asociado a un programa incompatible.'
                                USING ERRCODE = '23514';
                        END IF;
                    ELSIF TG_TABLE_NAME = 'PROGRAMA_ASIGNATURA' THEN
                        IF EXISTS (
                            SELECT 1
                            FROM "BLOQUE_PROGRAMA" AS bp
                            JOIN "BLOQUE_CONTENIDO" AS b
                              ON b."idBloque" = bp."idBloque"
                            WHERE bp."idPrograma" = OLD."idPrograma"
                              AND b."idAsignatura" = OLD."idAsignatura"
                              AND NOT EXISTS (
                                  SELECT 1
                                  FROM "PROGRAMA_ASIGNATURA" AS pa
                                  WHERE pa."idPrograma" = bp."idPrograma"
                                    AND pa."idAsignatura" = b."idAsignatura"
                              )
                        ) THEN
                            RAISE EXCEPTION 'RII03: no se puede quitar la asignatura del programa mientras tenga bloques asociados.'
                                USING ERRCODE = '23514';
                        END IF;
                    END IF;

                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER TR_BLOQUE_PROGRAMA_RII03
                BEFORE INSERT OR UPDATE ON "BLOQUE_PROGRAMA"
                FOR EACH ROW EXECUTE FUNCTION fn_m4_validar_rii03();

                CREATE TRIGGER TR_BLOQUE_CONTENIDO_RII03
                BEFORE UPDATE OF "idAsignatura" ON "BLOQUE_CONTENIDO"
                FOR EACH ROW EXECUTE FUNCTION fn_m4_validar_rii03();

                CREATE TRIGGER TR_PROGRAMA_ASIGNATURA_RII03
                AFTER UPDATE OR DELETE ON "PROGRAMA_ASIGNATURA"
                FOR EACH ROW EXECUTE FUNCTION fn_m4_validar_rii03();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TRIGGER IF EXISTS TR_BLOQUE_PROGRAMA_RII03 ON "BLOQUE_PROGRAMA";
                DROP TRIGGER IF EXISTS TR_BLOQUE_CONTENIDO_RII03 ON "BLOQUE_CONTENIDO";
                DROP TRIGGER IF EXISTS TR_PROGRAMA_ASIGNATURA_RII03 ON "PROGRAMA_ASIGNATURA";
                DROP FUNCTION IF EXISTS fn_m4_validar_rii03();
                """);

            migrationBuilder.DropTable(
                name: "BLOQUE_EN_HORARIO");

            migrationBuilder.DropTable(
                name: "BLOQUE_PROGRAMA");

            migrationBuilder.DropTable(
                name: "ESQUEMA_LLAVE");

            migrationBuilder.DropTable(
                name: "JEFE_DEPTO");

            migrationBuilder.DropTable(
                name: "MATRICULA");

            migrationBuilder.DropTable(
                name: "PROGRAMA_ASIGNATURA");

            migrationBuilder.DropTable(
                name: "AULA");

            migrationBuilder.DropTable(
                name: "HORARIO");

            migrationBuilder.DropTable(
                name: "BLOQUE_CONTENIDO");

            migrationBuilder.DropTable(
                name: "ESTUDIANTE");

            migrationBuilder.DropTable(
                name: "COORDINADOR_EN_PROGRAMA");

            migrationBuilder.DropTable(
                name: "ASIGNATURA");

            migrationBuilder.DropTable(
                name: "COORDINADOR");

            migrationBuilder.DropTable(
                name: "FECHA");

            migrationBuilder.DropTable(
                name: "PROGRAMA_ACADEMICO");

            migrationBuilder.DropTable(
                name: "PERSONA");
        }
    }
}
