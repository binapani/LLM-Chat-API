import sqlite3
import os

db = r'C:\Users\pc\OneDrive\Documents\GitHub\LLM-Chat-API\src\LLMChat.Api\vectors.db'

print('DB_EXISTS:', os.path.exists(db))

conn = sqlite3.connect(db)
cur = conn.cursor()

print('\nSCHEMA:')
print(cur.execute(
    "SELECT sql FROM sqlite_master WHERE name='DocumentChunksFts'"
).fetchone()[0])

print('\nCOUNT:')
print(cur.execute(
    "SELECT count(*) FROM DocumentChunksFts"
).fetchone()[0])

print('\nSAMPLE:')
for row in cur.execute(
    "SELECT DocumentId, ChunkId, Source, Content "
    "FROM DocumentChunksFts LIMIT 3"
):
    print(row)

print('\nVOCAB:')
for row in cur.execute(
    "SELECT * FROM fts5vocab('DocumentChunksFts', 'row') LIMIT 50"
):
    print(row)

queries = [
    'annual',
    'leave',
    'employees',
    'full',
    'time',
    'annual leave',
    'full employees',
    'full time employees',
    'full-time',
    '"annual leave"',
    '"full-time employees annual leave"',
    'full-time employees annual leave'
]

print('\nQUERY TESTS:')

for q in queries:
    try:
        count = cur.execute(
            "SELECT count(*) "
            "FROM DocumentChunksFts "
            "WHERE DocumentChunksFts MATCH ?",
            (q,)
        ).fetchone()[0]

        rows = cur.execute(
            "SELECT DocumentId, ChunkId, Source, "
            "bm25(DocumentChunksFts) AS score, Content "
            "FROM DocumentChunksFts "
            "WHERE DocumentChunksFts MATCH ? "
            "ORDER BY bm25(DocumentChunksFts) ASC "
            "LIMIT 5",
            (q,)
        ).fetchall()

        print('\nMATCH:', repr(q))
        print('COUNT:', count)
        print('TOP:', rows[:3])

    except Exception as e:
        print('\nMATCH:', repr(q))
        print('ERROR:', repr(e))

conn.close()