using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Timeblock.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// RII03 ante transacciones concurrentes. En READ COMMITTED, la validación
    /// de cada trigger no ve los cambios que otra transacción aún no confirmó,
    /// así que dos operaciones válidas por separado podían dejar un bloque en
    /// un programa que no imparte su asignatura. Ahora el trigger bloquea las
    /// filas en las que se basa su comprobación hasta el final de la
    /// transacción; la operación contraria espera y, al continuar, vuelve a
    /// validar con los datos ya confirmados.
    /// </summary>
    public partial class Rii03Concurrencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION fn_m4_validar_rii03() RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF TG_TABLE_NAME = 'BLOQUE_PROGRAMA' THEN
                        -- FOR SHARE sobre el bloque frena un cambio de su
                        -- asignatura; FOR KEY SHARE sobre la pertenencia frena
                        -- que se quite la asignatura del programa.
                        PERFORM 1
                        FROM "BLOQUE_CONTENIDO" AS b
                        JOIN "PROGRAMA_ASIGNATURA" AS pa
                          ON pa."idAsignatura" = b."idAsignatura"
                        WHERE b."idBloque" = NEW."idBloque"
                          AND pa."idPrograma" = NEW."idPrograma"
                        FOR SHARE OF b
                        FOR KEY SHARE OF pa;

                        IF NOT FOUND THEN
                            RAISE EXCEPTION 'RII03: la asignatura del bloque debe pertenecer al programa.'
                                USING ERRCODE = '23514';
                        END IF;
                    ELSIF TG_TABLE_NAME = 'BLOQUE_CONTENIDO' THEN
                        IF NEW."idAsignatura" IS DISTINCT FROM OLD."idAsignatura" THEN
                            -- Frena que se quite la nueva asignatura de los
                            -- programas del bloque mientras dura el cambio.
                            PERFORM 1
                            FROM "BLOQUE_PROGRAMA" AS bp
                            JOIN "PROGRAMA_ASIGNATURA" AS pa
                              ON pa."idPrograma" = bp."idPrograma"
                            WHERE bp."idBloque" = OLD."idBloque"
                              AND pa."idAsignatura" = NEW."idAsignatura"
                            FOR KEY SHARE OF pa;

                            IF EXISTS (
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
                        END IF;
                    ELSIF TG_TABLE_NAME = 'PROGRAMA_ASIGNATURA' THEN
                        -- No necesita bloqueos propios: las otras dos ramas
                        -- bloquean la fila que esta operación borra o cambia,
                        -- así que espera a que terminen y la consulta ya ve
                        -- sus cambios confirmados.
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
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Versión de InitialM4, sin bloqueos.
            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION fn_m4_validar_rii03() RETURNS trigger
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
                """);
        }
    }
}
