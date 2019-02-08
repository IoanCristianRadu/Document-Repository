CREATE SEQUENCE folders_id start with 1 increment by 1 nocache;
CREATE SEQUENCE content_data_id start with 1 increment by 1 nocache;
CREATE SEQUENCE file_details_id start with 1 increment by 1 nocache;
CREATE SEQUENCE app_settings_id start with 1 increment by 1 nocache;
--ON DELETE CASCADE

CREATE TABLE folders(
ID NUMBER(5) DEFAULT folders_id.NEXTVAL,
local_path VARCHAR2(255) NOT NULL, 
Parent_id NUMBER(5)
);

ALTER TABLE folders ADD CONSTRAINT pk_folders_id PRIMARY KEY (id);
ALTER TABLE folders ADD CONSTRAINT fk_parent_id FOREIGN KEY (parent_id) REFERENCES folders(id) ON DELETE CASCADE;
ALTER TABLE folders ADD (fullscan NUMBER(1) DEFAULT 1);

CREATE TABLE content_data(
ID NUMBER(5) DEFAULT content_data_id.NEXTVAL,
continut CLOB,
part_number NUMBER(2),
folder_id NUMBER(5),
file_details_id NUMBER(5)
);

ALTER TABLE content_data ADD CONSTRAINT pk_content_data PRIMARY KEY (id);
ALTER TABLE content_data ADD CONSTRAINT fk_folder_id FOREIGN KEY (folder_id) REFERENCES folders(id) ON DELETE CASCADE;


CREATE TABLE file_details(
ID NUMBER(5) DEFAULT file_details_id.NEXTVAL,
last_modified VARCHAR2(30),
date_created VARCHAR2(30),
filesize NUMBER(10),
extension VARCHAR2(6),
author VARCHAR2(60),
title VARCHAR2(60),
pages NUMBER(4),
folder_id NUMBER(5)
) ;


ALTER TABLE file_details ADD CONSTRAINT pk_file_details PRIMARY KEY (id);
ALTER TABLE file_details ADD CONSTRAINT fk_FD_folder_id FOREIGN KEY(folder_id) REFERENCES folders(id) ON DELETE CASCADE;

ALTER TABLE content_data ADD CONSTRAINT fk_file_details_id FOREIGN KEY (file_details_id) REFERENCES file_details(id) ON DELETE CASCADE;

CREATE TABLE app_settings(
ID NUMBER(2) DEFAULT app_settings_id.NEXTVAL,
filetype VARCHAR2(200),
last_index_date DATE,
convert_to VARCHAR2(300)
);

ALTER TABLE app_settings ADD CONSTRAINT pk_app_settings PRIMARY KEY (id);

INSERT INTO app_settings VALUES(default,'.pdf .txt .docx .xlsx .pptx .html',NULL,NULL);

CREATE INDEX idx_content ON content_data(continut)
     INDEXTYPE IS CTXSYS.CONTEXT
     PARAMETERS ('SYNC ( ON COMMIT)');
     
EXEC CTX_DDL.SYNC_INDEX('idx_content');

CREATE TABLE accounts(
email VARCHAR2(50) Primary Key,
password VARCHAR2(50) NOT NULL,
date_created VARCHAR2(30) NOT NULL,
type VARCHAR2(30) NOT NULL,
first_name VARCHAR2(50),
last_name VARCHAR2(50),
location VARCHAR2(50)
);

CREATE SEQUENCE groups_id start with 1 increment by 1 nocache;
CREATE TABLE groups(
groupID NUMBER(5) DEFAULT groups_id.NEXTVAL PRIMARY KEY,
group_name VARCHAR2(50) NOT NULL,
date_created VARCHAR2(30) NOT NULL
);


CREATE SEQUENCE group_members_id start with 1 increment by 1 nocache;
CREATE TABLE GroupMembers(
GroupMembersID NUMBER(5) DEFAULT group_members_id.NEXTVAL PRIMARY KEY,
groupID NUMBER(5),
accountEMAIL VARCHAR2(50)
);

ALTER TABLE groupmembers ADD CONSTRAINT fk_groupID FOREIGN KEY(groupID) REFERENCES groups(groupID) ON DELETE CASCADE;
ALTER TABLE groupmembers ADD CONSTRAINT fk_account_email FOREIGN KEY (accountEMAIL) REFERENCES accounts(email) ON DELETE CASCADE;

ALTER TABLE FILE_DETAILS
  ADD accountEmail VARCHAR2(50);
  
ALTER TABLE FILE_DETAILS ADD CONSTRAINT fk_fd_account_email FOREIGN KEY (accountEMAIL) REFERENCES accounts(email) ON DELETE CASCADE;
ALTER TABLE groupmembers ADD (member_type VARCHAR2(20));
