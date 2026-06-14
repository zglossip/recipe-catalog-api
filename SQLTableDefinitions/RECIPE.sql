CREATE TABLE recipe_catalog.recipe
(
    id smallserial NOT NULL,
    name character varying(100) NOT NULL,
    serving_amount smallint NOT NULL,
    serving_name character varying(50) NOT NULL,
    source character varying(100),
    uploaded timestamp not null default current_timestamp,
    parent_id smallint REFERENCES recipe_catalog.recipe (id) ON DELETE CASCADE
        CHECK (parent_id <> id),
    PRIMARY KEY (id)
);

CREATE FUNCTION recipe_catalog.enforce_single_nesting()
RETURNS trigger AS $$
BEGIN
    IF NEW.parent_id IS NOT NULL THEN
        -- the parent must not itself be a child
        IF EXISTS (SELECT 1 FROM recipe_catalog.recipe
                   WHERE id = NEW.parent_id AND parent_id IS NOT NULL) THEN
            RAISE EXCEPTION 'Cannot nest under recipe % because it is already a sub-recipe', NEW.parent_id;
        END IF;

        -- this recipe must not already have children
        IF EXISTS (SELECT 1 FROM recipe_catalog.recipe
                   WHERE parent_id = NEW.id) THEN
            RAISE EXCEPTION 'Recipe % already has sub-recipes and cannot become one', NEW.id;
        END IF;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER recipe_single_nesting
    BEFORE INSERT OR UPDATE ON recipe_catalog.recipe
    FOR EACH ROW EXECUTE FUNCTION recipe_catalog.enforce_single_nesting();