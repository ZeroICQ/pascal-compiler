var
   a: array [0..3] of array[0..3] of integer;
   i,j : integer;

begin
   for i:=0 to 3 do
      for j:=0 to 3 do
         a[i,j]:= i * j;

   for i:=0 to 3 do
   begin
      for j:=0 to 3 do
         write(a[i,j],' ');
      writeln;
   end;
end.