using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mottu.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MudarIdsParaIntComMigracaoDados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Executar toda a migração (EF Core gerencia a transação automaticamente)
            migrationBuilder.Sql(@"

                -- 1. MIGRAR ENTREGADORES
                ALTER TABLE ""Entregadores"" ADD COLUMN ""IdTemp"" INTEGER;
                
                UPDATE ""Entregadores""
                SET ""IdTemp"" = ABS(HASHTEXT(REPLACE(REPLACE(REPLACE(""Cnpj"", '.', ''), '/', ''), '-', '')));
                
                -- Resolver colisões
                DO $$
                DECLARE
                    r RECORD;
                    new_id INTEGER;
                    counter INTEGER;
                BEGIN
                    FOR r IN SELECT ""Id"", ""IdTemp"" FROM ""Entregadores"" ORDER BY ""IdTemp""
                    LOOP
                        new_id := r.""IdTemp"";
                        counter := 1;
                        WHILE EXISTS (SELECT 1 FROM ""Entregadores"" WHERE ""IdTemp"" = new_id AND ""Id"" != r.""Id"")
                        LOOP
                            new_id := r.""IdTemp"" + counter;
                            counter := counter + 1;
                        END LOOP;
                        UPDATE ""Entregadores"" SET ""IdTemp"" = new_id WHERE ""Id"" = r.""Id"";
                    END LOOP;
                END $$;

                -- 2. MIGRAR MOTOS
                ALTER TABLE ""Motos"" ADD COLUMN ""IdTemp"" INTEGER;
                
                UPDATE ""Motos""
                SET ""IdTemp"" = ABS(HASHTEXT(UPPER(""Placa"")));
                
                -- Resolver colisões
                DO $$
                DECLARE
                    r RECORD;
                    new_id INTEGER;
                    counter INTEGER;
                BEGIN
                    FOR r IN SELECT ""Id"", ""IdTemp"" FROM ""Motos"" ORDER BY ""IdTemp""
                    LOOP
                        new_id := r.""IdTemp"";
                        counter := 1;
                        WHILE EXISTS (SELECT 1 FROM ""Motos"" WHERE ""IdTemp"" = new_id AND ""Id"" != r.""Id"")
                        LOOP
                            new_id := r.""IdTemp"" + counter;
                            counter := counter + 1;
                        END LOOP;
                        UPDATE ""Motos"" SET ""IdTemp"" = new_id WHERE ""Id"" = r.""Id"";
                    END LOOP;
                END $$;

                -- 3. MIGRAR LOCACOES (adicionar colunas temporárias)
                ALTER TABLE ""Locacoes"" ADD COLUMN ""IdTemp"" INTEGER;
                ALTER TABLE ""Locacoes"" ADD COLUMN ""MotoIdTemp"" INTEGER;
                ALTER TABLE ""Locacoes"" ADD COLUMN ""EntregadorIdTemp"" INTEGER;
                
                -- Atualizar foreign keys temporárias
                UPDATE ""Locacoes"" l
                SET ""MotoIdTemp"" = m.""IdTemp""
                FROM ""Motos"" m
                WHERE l.""MotoId"" = m.""Id"";
                
                UPDATE ""Locacoes"" l
                SET ""EntregadorIdTemp"" = e.""IdTemp""
                FROM ""Entregadores"" e
                WHERE l.""EntregadorId"" = e.""Id"";
                
                -- Gerar IDs para Locacoes
                UPDATE ""Locacoes""
                SET ""IdTemp"" = ABS(HASHTEXT(
                    EXTRACT(EPOCH FROM ""DataInicio"")::text || 
                    COALESCE(""MotoIdTemp""::text, '') || 
                    COALESCE(""EntregadorIdTemp""::text, '')
                ));
                
                -- Resolver colisões para Locacoes
                DO $$
                DECLARE
                    r RECORD;
                    new_id INTEGER;
                    counter INTEGER;
                BEGIN
                    FOR r IN SELECT ""Id"", ""IdTemp"" FROM ""Locacoes"" ORDER BY ""IdTemp""
                    LOOP
                        new_id := r.""IdTemp"";
                        counter := 1;
                        WHILE EXISTS (SELECT 1 FROM ""Locacoes"" WHERE ""IdTemp"" = new_id AND ""Id"" != r.""Id"")
                        LOOP
                            new_id := r.""IdTemp"" + counter;
                            counter := counter + 1;
                        END LOOP;
                        UPDATE ""Locacoes"" SET ""IdTemp"" = new_id WHERE ""Id"" = r.""Id"";
                    END LOOP;
                END $$;

                -- 4. MIGRAR NOTIFICACOES
                ALTER TABLE ""Notificacoes"" ADD COLUMN ""IdTemp"" INTEGER;
                ALTER TABLE ""Notificacoes"" ADD COLUMN ""MotoIdTemp"" INTEGER;
                
                UPDATE ""Notificacoes"" n
                SET ""MotoIdTemp"" = m.""IdTemp""
                FROM ""Motos"" m
                WHERE n.""MotoId"" = m.""Id"";
                
                UPDATE ""Notificacoes""
                SET ""IdTemp"" = ABS(HASHTEXT(
                    EXTRACT(EPOCH FROM ""DataNotificacao"")::text || 
                    COALESCE(""MotoIdTemp""::text, '')
                ));
                
                -- Resolver colisões para Notificacoes
                DO $$
                DECLARE
                    r RECORD;
                    new_id INTEGER;
                    counter INTEGER;
                BEGIN
                    FOR r IN SELECT ""Id"", ""IdTemp"" FROM ""Notificacoes"" ORDER BY ""IdTemp""
                    LOOP
                        new_id := r.""IdTemp"";
                        counter := 1;
                        WHILE EXISTS (SELECT 1 FROM ""Notificacoes"" WHERE ""IdTemp"" = new_id AND ""Id"" != r.""Id"")
                        LOOP
                            new_id := r.""IdTemp"" + counter;
                            counter := counter + 1;
                        END LOOP;
                        UPDATE ""Notificacoes"" SET ""IdTemp"" = new_id WHERE ""Id"" = r.""Id"";
                    END LOOP;
                END $$;

                -- 5. REMOVER CONSTRAINTS E FOREIGN KEYS
                ALTER TABLE ""Locacoes"" DROP CONSTRAINT IF EXISTS ""FK_Locacoes_Entregadores_EntregadorId"";
                ALTER TABLE ""Locacoes"" DROP CONSTRAINT IF EXISTS ""FK_Locacoes_Motos_MotoId"";
                ALTER TABLE ""Notificacoes"" DROP CONSTRAINT IF EXISTS ""FK_Notificacoes_Motos_MotoId"";
                
                ALTER TABLE ""Entregadores"" DROP CONSTRAINT IF EXISTS ""PK_Entregadores"";
                ALTER TABLE ""Motos"" DROP CONSTRAINT IF EXISTS ""PK_Motos"";
                ALTER TABLE ""Locacoes"" DROP CONSTRAINT IF EXISTS ""PK_Locacoes"";
                ALTER TABLE ""Notificacoes"" DROP CONSTRAINT IF EXISTS ""PK_Notificacoes"";
                
                DROP INDEX IF EXISTS ""IX_Entregadores_Cnpj"";
                DROP INDEX IF EXISTS ""IX_Entregadores_NumeroCnh"";
                DROP INDEX IF EXISTS ""IX_Motos_Placa"";

                -- 6. REMOVER COLUNAS UUID E RENOMEAR TEMPORÁRIAS
                ALTER TABLE ""Entregadores"" DROP COLUMN ""Id"";
                ALTER TABLE ""Entregadores"" RENAME COLUMN ""IdTemp"" TO ""Id"";
                ALTER TABLE ""Entregadores"" ALTER COLUMN ""Id"" SET NOT NULL;
                
                ALTER TABLE ""Motos"" DROP COLUMN ""Id"";
                ALTER TABLE ""Motos"" RENAME COLUMN ""IdTemp"" TO ""Id"";
                ALTER TABLE ""Motos"" ALTER COLUMN ""Id"" SET NOT NULL;
                
                ALTER TABLE ""Locacoes"" DROP COLUMN ""Id"";
                ALTER TABLE ""Locacoes"" DROP COLUMN ""MotoId"";
                ALTER TABLE ""Locacoes"" DROP COLUMN ""EntregadorId"";
                ALTER TABLE ""Locacoes"" RENAME COLUMN ""IdTemp"" TO ""Id"";
                ALTER TABLE ""Locacoes"" RENAME COLUMN ""MotoIdTemp"" TO ""MotoId"";
                ALTER TABLE ""Locacoes"" RENAME COLUMN ""EntregadorIdTemp"" TO ""EntregadorId"";
                ALTER TABLE ""Locacoes"" ALTER COLUMN ""Id"" SET NOT NULL;
                ALTER TABLE ""Locacoes"" ALTER COLUMN ""MotoId"" SET NOT NULL;
                ALTER TABLE ""Locacoes"" ALTER COLUMN ""EntregadorId"" SET NOT NULL;
                
                ALTER TABLE ""Notificacoes"" DROP COLUMN ""Id"";
                ALTER TABLE ""Notificacoes"" DROP COLUMN ""MotoId"";
                ALTER TABLE ""Notificacoes"" RENAME COLUMN ""IdTemp"" TO ""Id"";
                ALTER TABLE ""Notificacoes"" RENAME COLUMN ""MotoIdTemp"" TO ""MotoId"";
                ALTER TABLE ""Notificacoes"" ALTER COLUMN ""Id"" SET NOT NULL;
                ALTER TABLE ""Notificacoes"" ALTER COLUMN ""MotoId"" SET NOT NULL;

                -- 7. RECRIAR CONSTRAINTS E ÍNDICES
                ALTER TABLE ""Entregadores"" ADD CONSTRAINT ""PK_Entregadores"" PRIMARY KEY (""Id"");
                CREATE UNIQUE INDEX ""IX_Entregadores_Cnpj"" ON ""Entregadores"" (""Cnpj"");
                CREATE UNIQUE INDEX ""IX_Entregadores_NumeroCnh"" ON ""Entregadores"" (""NumeroCnh"");
                
                ALTER TABLE ""Motos"" ADD CONSTRAINT ""PK_Motos"" PRIMARY KEY (""Id"");
                CREATE UNIQUE INDEX ""IX_Motos_Placa"" ON ""Motos"" (""Placa"");
                
                ALTER TABLE ""Locacoes"" ADD CONSTRAINT ""PK_Locacoes"" PRIMARY KEY (""Id"");
                ALTER TABLE ""Locacoes"" ADD CONSTRAINT ""FK_Locacoes_Entregadores_EntregadorId"" 
                    FOREIGN KEY (""EntregadorId"") REFERENCES ""Entregadores"" (""Id"") ON DELETE CASCADE;
                ALTER TABLE ""Locacoes"" ADD CONSTRAINT ""FK_Locacoes_Motos_MotoId"" 
                    FOREIGN KEY (""MotoId"") REFERENCES ""Motos"" (""Id"") ON DELETE CASCADE;
                
                ALTER TABLE ""Notificacoes"" ADD CONSTRAINT ""PK_Notificacoes"" PRIMARY KEY (""Id"");
                ALTER TABLE ""Notificacoes"" ADD CONSTRAINT ""FK_Notificacoes_Motos_MotoId"" 
                    FOREIGN KEY (""MotoId"") REFERENCES ""Motos"" (""Id"") ON DELETE CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotImplementedException("Rollback desta migration não é suportado automaticamente.");
        }
    }
}
