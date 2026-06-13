CREATE TABLE recipe_catalog.recipe
(
    id smallserial NOT NULL,
    name character varying(100) NOT NULL,
    serving_amount smallint NOT NULL,
    serving_name character varying(50) NOT NULL,
    source character varying(100),
    uploaded timestamp not null default current_timestamp,
    PRIMARY KEY (id)
);