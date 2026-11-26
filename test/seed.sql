create table customers (id int generated always as identity primary key, first_name text not null, last_name text not null, street_name text not null default '', street_number text not null default '', postal_code text not null default '', city text not null default '', email text not null default '', phone_number text not null default '', club text, note text);
create table bikes (id int generated always as identity primary key, customer_id int references customers(id), make text, model text, colour text, type text);
create table repairs (id int generated always as identity primary key, bike_id int references bikes(id), customer_id int references customers(id), total_price numeric(10,2) not null default 0, discount_type text, discount_value numeric(10,2) not null default 0, note text, created_at timestamptz default now(), intake_date timestamptz not null, delivery_date timestamptz, complete bool default false, payment_recieved bool default false, customer_contacted bool default false, missing_items bool default false);

insert into customers (first_name, last_name, phone_number, email) values
  ('Jens', 'Hansen', '20112233', 'jens@example.com'),
  ('Mette', 'Larsen', '30445566', 'mette@example.com');
insert into bikes (customer_id, make, model, colour, type) values
  (1, 'Kildemoes', 'City', 'Sort', 'city'),
  (2, 'Trek', 'Marlin 5', 'Rød', 'mtb');
insert into repairs (bike_id, customer_id, total_price, intake_date, delivery_date, complete, missing_items, note) values
  (1, 1, 450,  '2026-11-02', null,         false, true,  'Venter på bremsekabel'),
  (2, 2, 1200, '2026-11-05', '2026-11-08', true,  false, null),
  (1, 1, 300,  '2026-10-10', '2026-10-12', true,  false, null);

-- Til bike_history i 10.2
create table repair_items (id int generated always as identity primary key, repair_id int references repairs(id), title text not null, price numeric(10,2) not null default 0, quantity int not null default 1, total numeric(10,2) not null default 0, discount_type text, discount_value numeric(10,2) not null default 0);

insert into repair_items (repair_id, title, price, quantity, total) values
  (1, 'Bremsekabel',  150, 1, 150),
  (1, 'Arbejdsløn',   300, 1, 300),
  (2, 'Kæde',         350, 2, 700),
  (2, 'Service',      500, 1, 500),
  (3, 'Lapning',      150, 2, 300);

create role mcp_readonly with login password 'laese-kode';
grant usage on schema public to mcp_readonly;
grant select on customers, bikes, repairs, repair_items to mcp_readonly;