-- Somente leitura. Execute no schema da aplicação e compare com oracle-schema.sql.
SET LONG 100000
SET PAGESIZE 500
SET LINESIZE 220

SELECT USER AS schema_atual FROM DUAL;

SELECT banner_full FROM v$version;

SELECT table_name, column_name, data_type, data_precision, data_scale,
       char_length, nullable, identity_column
FROM user_tab_columns
WHERE table_name IN ('DONOS', 'FUNCIONARIOS', 'MEDICAMENTOS', 'PETS',
                     'CONSULTAS', 'VACINAS', 'CONSULTAS_MEDICAMENTOS', 'TUTORES')
ORDER BY table_name, column_id;

SELECT table_name, constraint_name, constraint_type, status, validated,
       r_constraint_name, delete_rule
FROM user_constraints
WHERE table_name IN ('DONOS', 'FUNCIONARIOS', 'MEDICAMENTOS', 'PETS',
                     'CONSULTAS', 'VACINAS', 'CONSULTAS_MEDICAMENTOS')
ORDER BY table_name, constraint_name;

SELECT table_name, index_name, uniqueness, status
FROM user_indexes
WHERE table_name IN ('DONOS', 'FUNCIONARIOS', 'MEDICAMENTOS', 'PETS',
                     'CONSULTAS', 'VACINAS', 'CONSULTAS_MEDICAMENTOS')
ORDER BY table_name, index_name;

-- Inclui os indices standalone: seus nomes nao provam quais colunas sao unicas.
SELECT i.table_name, i.index_name, i.uniqueness, c.column_position,
       c.column_name, c.descend
FROM user_indexes i
JOIN user_ind_columns c ON c.index_name = i.index_name AND c.table_name = i.table_name
WHERE i.table_name IN ('DONOS', 'FUNCIONARIOS', 'MEDICAMENTOS', 'PETS',
                       'CONSULTAS', 'VACINAS', 'CONSULTAS_MEDICAMENTOS')
ORDER BY i.table_name, i.index_name, c.column_position;

SELECT table_name, index_name, column_position, column_expression
FROM user_ind_expressions
WHERE table_name IN ('DONOS', 'FUNCIONARIOS', 'MEDICAMENTOS', 'PETS',
                     'CONSULTAS', 'VACINAS', 'CONSULTAS_MEDICAMENTOS')
ORDER BY table_name, index_name, column_position;

SELECT DBMS_METADATA.GET_DDL('INDEX', index_name) AS ddl_indice_existente
FROM user_indexes
WHERE table_name IN ('DONOS', 'FUNCIONARIOS', 'MEDICAMENTOS', 'PETS',
                     'CONSULTAS', 'VACINAS', 'CONSULTAS_MEDICAMENTOS')
ORDER BY table_name, index_name;

SELECT table_name, column_name, generation_type, sequence_name, identity_options
FROM user_tab_identity_cols
WHERE table_name IN ('DONOS', 'FUNCIONARIOS', 'MEDICAMENTOS', 'PETS',
                     'CONSULTAS', 'VACINAS', 'CONSULTAS_MEDICAMENTOS')
ORDER BY table_name, column_name;

-- LAST_NUMBER e limite de cache; nao significa o proximo ID garantido.
-- Apenas consultar metadados: nao consome NEXTVAL nem altera sequencias.
SELECT i.table_name, i.column_name, s.sequence_name, s.increment_by,
       s.min_value, s.max_value, s.cache_size, s.last_number
FROM user_tab_identity_cols i
JOIN user_sequences s ON s.sequence_name = i.sequence_name
WHERE i.table_name IN ('DONOS', 'FUNCIONARIOS', 'MEDICAMENTOS', 'PETS',
                       'CONSULTAS', 'VACINAS', 'CONSULTAS_MEDICAMENTOS')
ORDER BY i.table_name, i.column_name;

SELECT DBMS_METADATA.GET_DDL('TABLE', table_name) AS ddl_existente
FROM user_tables
WHERE table_name IN ('DONOS', 'FUNCIONARIOS', 'MEDICAMENTOS', 'PETS',
                     'CONSULTAS', 'VACINAS', 'CONSULTAS_MEDICAMENTOS')
ORDER BY table_name;

SELECT table_name FROM user_tables WHERE table_name = '__EFMigrationsHistory';

-- Executa apenas se o historico existir; nao cria nem altera objetos.
VARIABLE migration_history REFCURSOR
DECLARE
    history_exists NUMBER;
BEGIN
    SELECT COUNT(*) INTO history_exists
    FROM user_tables WHERE table_name = '__EFMigrationsHistory';
    IF history_exists > 0 THEN
        OPEN :migration_history FOR
            'SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId"';
    ELSE
        OPEN :migration_history FOR
            SELECT 'Historico EF ausente' AS resultado FROM DUAL;
    END IF;
END;
/
PRINT migration_history
