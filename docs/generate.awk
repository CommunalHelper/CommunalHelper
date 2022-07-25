BEGIN {
    FS=""
}

function trim(str) {
    gsub(/^ +/, "", str)
    gsub(/ +$/, "", str)
    return str
}

function value(str) {
    val = substr(str,index(str,"=")+1)
    return trim(val)
}

function entity(str) {
    split(str, a, ".")
    return a[2]
}

function key(str) {
    match(str,/\.([^.]+)=/,a)
    return a[1]
}

function humanize(str) {
    sub(/([A-Z])/, " &",str)
    str = trim(str)
    return toupper(substr(str,1,1)) substr(str,2)
}

/^#/ {
    name = substr($0, 3)
    printID = 1
    print "## " name
}

/placements|attributes/ {
    if (printID) {
        print "*`" entity($0) "`*\n"
        printID = 0
    }
}

/placements.name/ {
    if (name != value($0)) {
        print "### " value($0)
    }
}

/placements.description/ {
    print value($0) "\n"
}

/attributes.description/ {
    print "**" humanize(key($0)) "**: " value($0) "\n"
}
