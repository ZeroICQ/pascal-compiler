type
    ElType = double;

var
    a: array [0..20] of ElType;
    n: integer = 20;
    i, j: integer;
    x: ElType;

procedure fill_array;
begin
    for i := -n to 0 do begin
        a[i + n] := -i;
    end;
end;

procedure print_array;
begin
    for i := 0 to n do
        write(a[i], ' ' );
    writeln;
end;

procedure bubble_sort;
begin
    for i := 0 to n do
        for j := i downto 1 do
            if a[j] < a[j-1] then begin
                x := a[j];
                a[j] := a[j-1];
                a[j-1] := x;
            end;
end;

procedure insertion_sort;
begin
    for i := 1 to n - 1 do begin
        x := a[i];
        j := i;
        while (j > 0) and (a[j-1] > x) do begin
            a[j] := a[j-1];
            j := j - 1;
        end;
        a[j] := x;
    end;
end;

begin
    fill_array();
    print_array();
    bubble_sort();
    print_array();
    writeln;

    fill_array();
    print_array();
    insertion_sort();
    print_array();
    writeln;


end.