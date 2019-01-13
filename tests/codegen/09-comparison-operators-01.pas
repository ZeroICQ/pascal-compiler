var 
    da, db, dc: double;
    ia, ib, ic: integer;
begin
    da := 10;
    db := 10.1;
    dc := -10.1;

    ia := 128;
    ib := 18;
    ic := -1008;
    //double double
    writeln(da < da);
    writeln(da < db);
    writeln(da < dc);

    writeln(db < da);
    writeln(db < db);
    writeln(db < dc);

    writeln(dc < da);
    writeln(dc < db);
    writeln(dc < dc);

    //int-int
    writeln(ia < ia);
    writeln(ia < ib);
    writeln(ia < ic);

    writeln(ib < ia);
    writeln(ib < ib);
    writeln(ib < ic);

    writeln(ic < ia);
    writeln(ic < ib);
    writeln(ic < ic);

    //double-int
    writeln(da < ia);
    writeln(da < ib);
    writeln(da < ic);

    writeln(db < ia);
    writeln(db < ib);
    writeln(db < ic);

    writeln(dc < ia);
    writeln(dc < ib);
    writeln(dc < ic);

    //int-double
    writeln(ia < da);
    writeln(ia < db);
    writeln(ia < dc);

    writeln(ib < da);
    writeln(ib < db);
    writeln(ib < dc);

    writeln(ic < da);
    writeln(ic < db);
    writeln(ic < dc);
//----
//
//
//
//
//
//
//
//
    writeln(da > da);
    writeln(da > db);
    writeln(da > dc);

    writeln(db > da);
    writeln(db > db);
    writeln(db > dc);

    writeln(dc > da);
    writeln(dc > db);
    writeln(dc > dc);

    //int-int
    writeln(ia > ia);
    writeln(ia > ib);
    writeln(ia > ic);

    writeln(ib > ia);
    writeln(ib > ib);
    writeln(ib > ic);

    writeln(ic > ia);
    writeln(ic > ib);
    writeln(ic > ic);

    //double-int
    writeln(da > ia);
    writeln(da > ib);
    writeln(da > ic);

    writeln(db > ia);
    writeln(db > ib);
    writeln(db > ic);

    writeln(dc > ia);
    writeln(dc > ib);
    writeln(dc > ic);

    //int-double
    writeln(ia > da);
    writeln(ia > db);
    writeln(ia > dc);

    writeln(ib > da);
    writeln(ib > db);
    writeln(ib > dc);

    writeln(ic > da);
    writeln(ic > db);
    writeln(ic > dc);

    //
//
//
//
//
//
//
//
    writeln(da <= da);
    writeln(da <= db);
    writeln(da <= dc);

    writeln(db <= da);
    writeln(db <= db);
    writeln(db <= dc);

    writeln(dc <= da);
    writeln(dc <= db);
    writeln(dc <= dc);

    //int-int
    writeln(ia <= ia);
    writeln(ia <= ib);
    writeln(ia <= ic);

    writeln(ib <= ia);
    writeln(ib <= ib);
    writeln(ib <= ic);

    writeln(ic <= ia);
    writeln(ic <= ib);
    writeln(ic <= ic);

    //double-int
    writeln(da <= ia);
    writeln(da <= ib);
    writeln(da <= ic);

    writeln(db <= ia);
    writeln(db <= ib);
    writeln(db <= ic);

    writeln(dc <= ia);
    writeln(dc <= ib);
    writeln(dc <= ic);

    //int-double
    writeln(ia <= da);
    writeln(ia <= db);
    writeln(ia <= dc);

    writeln(ib <= da);
    writeln(ib <= db);
    writeln(ib <= dc);

    writeln(ic <= da);
    writeln(ic <= db);
    writeln(ic <= dc);

//
//
//
//
//
//
//
    writeln(da >= da);
    writeln(da >= db);
    writeln(da >= dc);

    writeln(db >= da);
    writeln(db >= db);
    writeln(db >= dc);

    writeln(dc >= da);
    writeln(dc >= db);
    writeln(dc >= dc);

    //int-int
    writeln(ia >= ia);
    writeln(ia >= ib);
    writeln(ia >= ic);

    writeln(ib >= ia);
    writeln(ib >= ib);
    writeln(ib >= ic);

    writeln(ic >= ia);
    writeln(ic >= ib);
    writeln(ic >= ic);

    //double-int
    writeln(da >= ia);
    writeln(da >= ib);
    writeln(da >= ic);

    writeln(db >= ia);
    writeln(db >= ib);
    writeln(db >= ic);

    writeln(dc >= ia);
    writeln(dc >= ib);
    writeln(dc >= ic);

    //int-double
    writeln(ia >= da);
    writeln(ia >= db);
    writeln(ia >= dc);

    writeln(ib >= da);
    writeln(ib >= db);
    writeln(ib >= dc);

    writeln(ic >= da);
    writeln(ic >= db);
    writeln(ic >= dc);

//
//
//
//
//
//
//
    writeln(da <> da);
    writeln(da <> db);
    writeln(da <> dc);

    writeln(db <> da);
    writeln(db <> db);
    writeln(db <> dc);

    writeln(dc <> da);
    writeln(dc <> db);
    writeln(dc <> dc);

    //int-int
    writeln(ia <> ia);
    writeln(ia <> ib);
    writeln(ia <> ic);

    writeln(ib <> ia);
    writeln(ib <> ib);
    writeln(ib <> ic);

    writeln(ic <> ia);
    writeln(ic <> ib);
    writeln(ic <> ic);

    //double-int
    writeln(da <> ia);
    writeln(da <> ib);
    writeln(da <> ic);

    writeln(db <> ia);
    writeln(db <> ib);
    writeln(db <> ic);

    writeln(dc <> ia);
    writeln(dc <> ib);
    writeln(dc <> ic);

    //int-double
    writeln(ia <> da);
    writeln(ia <> db);
    writeln(ia <> dc);

    writeln(ib <> da);
    writeln(ib <> db);
    writeln(ib <> dc);

    writeln(ic <> da);
    writeln(ic <> db);
    writeln(ic <> dc);

    //
//
//
//
//
//
//
    writeln(da = da);
    writeln(da = db);
    writeln(da = dc);

    writeln(db = da);
    writeln(db = db);
    writeln(db = dc);

    writeln(dc = da);
    writeln(dc = db);
    writeln(dc = dc);

    //int-int
    writeln(ia = ia);
    writeln(ia = ib);
    writeln(ia = ic);

    writeln(ib = ia);
    writeln(ib = ib);
    writeln(ib = ic);

    writeln(ic = ia);
    writeln(ic = ib);
    writeln(ic = ic);

    //double-int
    writeln(da = ia);
    writeln(da = ib);
    writeln(da = ic);

    writeln(db = ia);
    writeln(db = ib);
    writeln(db = ic);

    writeln(dc = ia);
    writeln(dc = ib);
    writeln(dc = ic);

    //int-double
    writeln(ia = da);
    writeln(ia = db);
    writeln(ia = dc);

    writeln(ib = da);
    writeln(ib = db);
    writeln(ib = dc);

    writeln(ic = da);
    writeln(ic = db);
    writeln(ic = dc);


end.