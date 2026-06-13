CREATE TABLE recipe_catalog.ingredient
(
    recipe_id smallint NOT NULL,
    "position" smallint NOT NULL,
    name character varying(100) NOT NULL,
    uom character varying(30),
    quantity numeric(10, 3) NOT NULL,
    notes character varying(500),
    PRIMARY KEY (recipe_id, name),
    CONSTRAINT "recipe_FK" FOREIGN KEY (recipe_id)
        REFERENCES recipe_catalog.recipe (id) MATCH SIMPLE
        ON UPDATE CASCADE
        ON DELETE CASCADE
        NOT VALID
);
