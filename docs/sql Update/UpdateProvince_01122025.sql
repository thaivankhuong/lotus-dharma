


CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS unaccent;
SELECT similarity('abc', 'abcd');



ALTER TABLE provinces ADD COLUMN name_unaccent TEXT;


CREATE FUNCTION update_unaccent_province()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    NEW.name_unaccent := unaccent(NEW.name);
    RETURN NEW;
END;
$$;



CREATE TRIGGER trg_unaccent_province
BEFORE INSERT OR UPDATE ON provinces
FOR EACH ROW
EXECUTE FUNCTION update_unaccent_province();

UPDATE provinces SET name_unaccent = unaccent(name);


