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
    user_id uuid not null references finance.user_account(id) on delete cascade,
    conversation_key text not null,
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
    user_id uuid not null references finance.user_account(id) on delete cascade,
    name text not null,
    currency_code text not null,
    opening_amount numeric(19, 4) not null,
    current_amount numeric(19, 4) not null,
    created_utc timestamptz not null,
    updated_utc timestamptz not null
);

create table if not exists finance.category
(
    id uuid primary key,
    kind text not null,
    scope text not null,
    user_id uuid null references finance.user_account(id) on delete cascade,
    code text null,
    name text not null,
    created_utc timestamptz not null,
    updated_utc timestamptz not null
);

create table if not exists finance.transaction_entry
(
    id uuid primary key,
    user_id uuid not null references finance.user_account(id) on delete cascade,
    account_id uuid not null references finance.account(id) on delete cascade,
    category_id uuid not null references finance.category(id) on delete cascade,
    kind text not null,
    amount numeric(19, 4) not null,
    occurred_utc timestamptz not null,
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
create unique index if not exists ux_workspace_user_conversation on finance.workspace(user_id, conversation_key);
create index if not exists idx_account_user on finance.account(user_id);
create unique index if not exists ux_account_user_name on finance.account(user_id, lower(name));
create index if not exists idx_category_user on finance.category(user_id) where user_id is not null;
create unique index if not exists ux_category_system_code on finance.category(kind, code) where user_id is null;
create unique index if not exists ux_category_user_name on finance.category(user_id, kind, lower(name)) where user_id is not null;
create index if not exists idx_transaction_entry_user_occurred on finance.transaction_entry(user_id, occurred_utc desc);
drop index if exists finance.idx_inbox_pending;
drop index if exists finance.idx_outbox_pending;
create index if not exists idx_inbox_pending on finance.inbox_message(received_utc) where processed_utc is null;
create index if not exists idx_outbox_pending on finance.outbox_message(created_utc) where published_utc is null;

with seed(id, code, name) as
(
    values
        ('11111111-1111-1111-1111-111111111111', 'food', 'Food'),
        ('22222222-2222-2222-2222-222222222222', 'transport', 'Transport'),
        ('33333333-3333-3333-3333-333333333333', 'home', 'Home'),
        ('44444444-4444-4444-4444-444444444444', 'health', 'Health'),
        ('55555555-5555-5555-5555-555555555555', 'shopping', 'Shopping'),
        ('66666666-6666-6666-6666-666666666666', 'fun', 'Fun'),
        ('77777777-7777-7777-7777-777777777777', 'bills', 'Bills'),
        ('88888888-8888-8888-8888-888888888888', 'travel', 'Travel')
),
meta(kind, scope, created_utc) as
(
    values ('expense', 'system', '2026-01-01 00:00:00+00'::timestamptz)
)
insert into finance.category(id, kind, scope, user_id, code, name, created_utc, updated_utc)
select seed.id::uuid, meta.kind, meta.scope, null, seed.code, seed.name, meta.created_utc, meta.created_utc
from seed
cross join meta
on conflict do nothing;
