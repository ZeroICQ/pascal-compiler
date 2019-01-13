var
    i, j, k: integer;
begin
    for i := 1 to 100 do begin
        k += 4;
    end;
    writeln(k);


    k := 0;
    for i := 1 to 1000 do begin
        k += 4;
        if i = 100 then
            break;
    end;
    writeln(k);


    k := 0;
    for i := 200 downto 1 do begin
        if i mod 2 <> 0 then
            continue;
        k += 4;
        for j := 1 to 1000 do begin
            break;
            k += 1;
        end;
    end;
    writeln(k);


    for i := 1 to 1 do
        writeln(k);


    for i := 1 to 0 do
        writeln(0);


    for i := 0 downto 1 do
        writeln(0);
end.