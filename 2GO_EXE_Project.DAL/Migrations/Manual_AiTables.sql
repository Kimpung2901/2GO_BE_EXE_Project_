-- Manual SQL for AI tables (PostgreSQL)
CREATE TABLE IF NOT EXISTS "MarketPrices" (
    "MarketPriceId" serial PRIMARY KEY,
    "ProductKey" varchar(255) NOT NULL,
    "CategoryId" integer NULL,
    "Condition" varchar(20) NOT NULL,
    "AvgPrice" numeric(15,2) NOT NULL,
    "MinPrice" numeric(15,2) NOT NULL,
    "MaxPrice" numeric(15,2) NOT NULL,
    "SampleCount" integer NOT NULL DEFAULT 0,
    "Source" varchar(50) NULL,
    "Confidence" varchar(20) NULL,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
    UNIQUE ("ProductKey", "CategoryId", "Condition")
);

CREATE TABLE IF NOT EXISTS "AIAnalysisLog" (
    "LogId" bigserial PRIMARY KEY,
    "Type" varchar(50) NULL,
    "RequestJson" text NULL,
    "ResponseJson" text NULL,
    "UserId" varchar(100) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS "ManualReviewQueue" (
    "QueueId" bigserial PRIMARY KEY,
    "ListingId" bigint NULL,
    "Reason" varchar(255) NULL,
    "Status" varchar(50) NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW()
);