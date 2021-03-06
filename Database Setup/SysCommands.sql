
create user C##DocRep identified by DocRep;

--GRANT RESOURCE, CONNECT, CTXAPP, TO C##DocRep;
GRANT RESOURCE, CONNECT, CTXAPP TO C##DocRep;
GRANT ALL PRIVILEGES TO C##DocRep;

GRANT EXECUTE ON CTXSYS.CTX_CLS TO C##DocRep;
GRANT EXECUTE ON CTXSYS.CTX_DDL TO C##DocRep;
GRANT EXECUTE ON CTXSYS.CTX_DOC TO C##DocRep;
GRANT EXECUTE ON CTXSYS.CTX_OUTPUT TO C##DocRep;
GRANT EXECUTE ON CTXSYS.CTX_QUERY TO C##DocRep;
GRANT EXECUTE ON CTXSYS.CTX_REPORT TO C##DocRep;
GRANT EXECUTE ON CTXSYS.CTX_THES TO C##DocRep;
GRANT EXECUTE ON CTXSYS.CTX_ULEXER TO C##DocRep;