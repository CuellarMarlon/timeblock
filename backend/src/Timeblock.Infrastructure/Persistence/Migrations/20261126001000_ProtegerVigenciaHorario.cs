using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Timeblock.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TimeblockDbContext))]
[Migration("20261126001000_ProtegerVigenciaHorario")]
public sealed class ProtegerVigenciaHorario : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM "HORARIO" h JOIN "COORDINADOR_EN_PROGRAMA" a
                      ON a."idCoordinador" = h."idCoordinadorGen" AND a."idPrograma" = h."idPrograma"
                     AND a."fechaInicio" = h."fechaInicioGen"
                    WHERE NOT a."puedeGenerar" OR (h."fechaCreacion" AT TIME ZONE 'UTC')::date < a."fechaInicio"
                       OR (a."fechaFin" IS NOT NULL AND (h."fechaCreacion" AT TIME ZONE 'UTC')::date > a."fechaFin")
                ) THEN
                    RAISE EXCEPTION 'Existen horarios incompatibles con la vigencia o permiso de su autorización.';
                END IF;
            END $$;

            CREATE FUNCTION timeblock_verificar_autorizacion_horario() RETURNS trigger LANGUAGE plpgsql AS $$
            DECLARE autorizacion "COORDINADOR_EN_PROGRAMA"%ROWTYPE;
            BEGIN
                -- FOR SHARE también bloquea cambios de permiso y vigencia no ligados a la clave.
                SELECT * INTO autorizacion FROM "COORDINADOR_EN_PROGRAMA"
                 WHERE "idCoordinador" = NEW."idCoordinadorGen" AND "idPrograma" = NEW."idPrograma"
                   AND "fechaInicio" = NEW."fechaInicioGen" FOR SHARE;
                IF FOUND AND (NOT autorizacion."puedeGenerar"
                    OR (NEW."fechaCreacion" AT TIME ZONE 'UTC')::date < autorizacion."fechaInicio"
                    OR (autorizacion."fechaFin" IS NOT NULL AND (NEW."fechaCreacion" AT TIME ZONE 'UTC')::date > autorizacion."fechaFin")) THEN
                    RAISE EXCEPTION 'AUTORIZACION_HORARIO: la autorización no permite generar en la fecha indicada.' USING ERRCODE = '23514';
                END IF;
                RETURN NEW;
            END $$;

            CREATE TRIGGER "TR_HORARIO_AutorizacionVigente"
              BEFORE INSERT OR UPDATE OF "idCoordinadorGen", "idPrograma", "fechaInicioGen", "fechaCreacion"
              ON "HORARIO" FOR EACH ROW EXECUTE FUNCTION timeblock_verificar_autorizacion_horario();

            CREATE FUNCTION timeblock_proteger_autorizacion_utilizada() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM "HORARIO" h
                     WHERE h."idCoordinadorGen" = OLD."idCoordinador" AND h."idPrograma" = OLD."idPrograma"
                       AND h."fechaInicioGen" = OLD."fechaInicio"
                       AND (NOT NEW."puedeGenerar" OR (NEW."fechaFin" IS NOT NULL
                           AND (h."fechaCreacion" AT TIME ZONE 'UTC')::date > NEW."fechaFin"))
                ) THEN
                    RAISE EXCEPTION 'AUTORIZACION_HORARIO: el cambio invalidaría horarios existentes.' USING ERRCODE = '23514';
                END IF;
                RETURN NEW;
            END $$;
            CREATE TRIGGER "TR_AUTORIZACION_ProtegerHorarios"
              BEFORE UPDATE OF "fechaFin", "puedeGenerar" ON "COORDINADOR_EN_PROGRAMA"
              FOR EACH ROW EXECUTE FUNCTION timeblock_proteger_autorizacion_utilizada();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER "TR_AUTORIZACION_ProtegerHorarios" ON "COORDINADOR_EN_PROGRAMA";
            DROP FUNCTION timeblock_proteger_autorizacion_utilizada();
            DROP TRIGGER "TR_HORARIO_AutorizacionVigente" ON "HORARIO";
            DROP FUNCTION timeblock_verificar_autorizacion_horario();
            """);
    }
}
