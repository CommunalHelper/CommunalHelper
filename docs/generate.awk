# gawk --lint=no-ext -f docs/generate.awk Loenn/lang/en_gb.lang > $TEMPFILE
#
# Variables:
# -v METAFILE - file to output additional metadata (Table of Contents)


BEGIN {
    FS=""
    # warning: reference to uninitialized variable `METAFILE`
    if (!METAFILE) {
        METAFILE="/dev/null"
    }
    print "# Table of Contents\n<details>\n<summary>Click to expand Table of Contents</summary>\n" > METAFILE
}

function trim(str) {
    gsub(/^ +/, "", str)
    gsub(/ +$/, "", str)
    return str
}

function value(str) {
    val = substr(str,index(str,"=")+1)
    gsub(/\\n/, "\n", val)
    gsub(/</, "\\&lt;", val)
    gsub(/>/, "\\&gt;", val)
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

function toAnchor(str) {
    str = tolower(str)
    gsub(/[^0-9A-Za-z_ -]/, "", str)
    gsub(/ /, "-", str)
    return str
}

/^#/ {
    name = substr($0, 3)
    delete attributes
    printID = 1
    print "## " name

    print "- [" name "](#" toAnchor(name) ")" > METAFILE
}

/placements|attributes/ {
    if (printID) {
        print "*`" entity($0) "`*\n"
        printID = 0
    }
}

/placements.name/ {
    if (name != value($0)) {
        print "- ### " value($0)
    }
}

/placements.description/ {
    print value($0) "\n"
}

/attributes.description/ {
    attr = key($0)
    if (!(attr in attributes)) {
        print "**" humanize(attr) "**: " value($0) "\n"
    }
    attributes[attr] = 1
}

END {
    print "\n</details>\n\n# Reference" > METAFILE
    close(METAFILE)
}
