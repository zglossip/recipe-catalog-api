CREATE TABLE recipe_catalog.tag
(
    recipe_id smallint NOT NULL,
    text character varying(50) NOT NULL,
    PRIMARY KEY (recipe_id, text),
    CONSTRAINT "recipe_FK" FOREIGN KEY (recipe_id)
        REFERENCES recipe_catalog.recipe (id) MATCH SIMPLE
        ON UPDATE CASCADE
        ON DELETE CASCADE
        NOT VALID
);