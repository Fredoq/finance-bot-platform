create schema if not exists finance;

create table if not exists finance.user_account
(
    id uuid primary key,
    actor_key text not null unique,
    name text not null,
    locale text not null,
    created_utc timestamptz not null,
    updated_utc timestamptz not null
);

create table if not exists finance.workspace
(
    id uuid primary key,
    user_id uuid not null references finance.user_account(id),
    conversation_key text not null unique,
    state_code text not null,
    state_data jsonb not null default '{}'::jsonb,
    revision bigint not null,
    entry_payload text not null,
    last_payload text not null,
    created_utc timestamptz not null,
    opened_utc timestamptz not null,
    updated_utc timestamptz not null
);

create table if not exists finance.account
(
    id uuid primary key,
    user_id uuid not null references finance.user_account(id),
    name text not null,
    currency_code text not null,
    opening_amount numeric(19, 4) not null,
    current_amount numeric(19, 4) not null,
    created_utc timestamptz not null,
    updated_utc timestamptz not null
);

create table if not exists finance.inbox_message
(
    message_id uuid primary key,
    contract text not null,
    source text not null,
    correlation_id text not null,
    causation_id text not null,
    idempotency_key text not null,
    payload jsonb not null,
    received_utc timestamptz not null,
    processed_utc timestamptz null,
    attempt integer not null,
    unique (contract, idempotency_key)
);

create table if not exists finance.outbox_message
(
    message_id uuid primary key,
    contract text not null,
    routing_key text not null,
    source text not null,
    correlation_id text not null,
    causation_id text not null,
    idempotency_key text not null,
    payload jsonb not null,
    occurred_utc timestamptz not null,
    created_utc timestamptz not null,
    published_utc timestamptz null,
    attempt integer not null,
    error text not null,
    unique (contract, idempotency_key)
);

create index if not exists idx_workspace_user on finance.workspace(user_id);
create index if not exists idx_account_user on finance.account(user_id);
create unique index if not exists ux_account_user_name on finance.account(user_id, lower(name));
drop index if exists finance.idx_inbox_pending;
drop index if exists finance.idx_outbox_pending;
create index if not exists idx_inbox_pending on finance.inbox_message(received_utc) where processed_utc is null;
create index if not exists idx_outbox_pending on finance.outbox_message(created_utc) where published_utc is null;
