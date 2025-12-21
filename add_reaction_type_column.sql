-- SQL script to add ReactionType column to PostLikes table
-- Run this directly on your MySQL database

ALTER TABLE PostLikes 
ADD COLUMN ReactionType VARCHAR(255) NULL;

-- Verify the column was added
-- SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE 
-- FROM INFORMATION_SCHEMA.COLUMNS 
-- WHERE TABLE_NAME = 'PostLikes' AND COLUMN_NAME = 'ReactionType';




