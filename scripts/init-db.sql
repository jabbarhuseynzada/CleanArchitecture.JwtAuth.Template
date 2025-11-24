-- Initial database setup script
-- This script runs automatically when PostgreSQL container starts for the first time

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE "WebTemplateDb" TO postgres;

-- Log successful initialization
DO $$
BEGIN
    RAISE NOTICE 'Database WebTemplateDb initialized successfully!';
END $$;
