#! /usr/bin/perl -w

# Copyright (C) 2002 Simon Josefsson

# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2, or (at your option)
# any later version.

# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.

# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA
# 02111-1307, USA.

# I consider the output of this program to be unrestricted.  Use it as
# you will.

# translation to C# output by Joe Hildebrand

use strict;

my ($tab) = 59;
my ($intable) = 0;
my ($tablename);
my ($varname);
my ($starheader, $header);
my ($profile) = "RFC3454";
my ($filename) = "${profile}.cs";
my ($line, $start, $end, @map);

open(FH, ">$filename") or die "cannot open $filename for writing";

print FH <<EOF;
/* This file is auto-generated.  DO NOT EDIT! */

using System;
namespace stringprep
{
    /// <summary>
	/// Constants from RFC 3454, Stringprep.
	/// </summary>
    public class RFC3454
    {
EOF

while(<>) {
    s/^   (.*)/$1/g; # for rfc
    $line = $_;

    die "already in table" if $intable && m,^----- Start Table (.*) -----,;
    die "not in table" if !$intable && m,^----- End Table (.*) -----,;

    if ($intable && m,^----- End Table (.*) -----,) {
	die "table error" unless $1 eq $tablename ||
	    ($1 eq "C.1.2" && $tablename eq "C.1.1"); # Typo in draft
	$intable = 0;
	print FH "        };\n\n";
    }

    if (m,^[A-Z],) {
	$header = $line;
    } elsif (!m,^[ -],) {
	$header .= $line;
    }

    next unless ($intable || m,^----- Start Table (.*) -----,);

    if ($intable) {
	$_ = $line;
	chop $line;

	next if m,^$,;
	next if m,^Hoffman & Blanchet          Standards Track                    \[Page [0-9]+\]$,;
	next if m,^$,;
	next if m,RFC 3454        Preparation of Internationalized Strings   December 2002,;

	die "regexp failed on line: $line" unless
	    m,^([0-9A-F]+)(-([0-9A-F]+))?(; ([0-9A-F]+)( ([0-9A-F]+))?( ([0-9A-F]+))?( ([0-9A-F]+))?;)?,;

	die "too many mapping targets on line: $line" if $12;

	$start = $1;
	$end = $3;
	$map[0] = $5;
	$map[1] = $7;
	$map[2] = $9;
	$map[3] = $11;

	die "tables tried to map a range" if $end && $map[0];

	if (length($start) > 4) {
	  # do nothing
	} elsif ($map[3]) {
	    printf FH "            \"\\x%04s\\x%04s\\x%04s\\x%04s\\x%04s\", %*s/* %s */\n",
	    $start, $map[0], $map[1], $map[2], $map[3], $tab-length($line)-13, " ", $line;
	} elsif ($map[2]) {
	    printf FH "            \"\\x%04s\\x%04s\\x%04s\\x%04s\", %*s/* %s */\n",
	    $start, $map[0], $map[1], $map[2], $tab-length($line)-20, " ", $line;
	} elsif ($map[1]) {
	    printf FH "            \"\\x%04s\\x%04s\\x%04s\", %*s/* %s */\n",
	    $start, $map[0], $map[1], $tab-length($line)-19, " ", $line,
	} elsif ($map[0]) {
	    printf FH "            \"\\x%04s\\x%04s\",%*s/* %s */\n",
	    $start, $map[0], $tab-length($line)-17, " ",  $line;
	} elsif ($end) {
	    printf FH "            new char[] {'\\x%04s', '\\x%04s'},%*s/* %s */\n",
	    $start, $end, $tab-length($line)-46, " ",  $line;
	} else {
		if ($varname =~ /^B/) {
	        printf FH "            \"\\x%04s\",%*s/* %s */\n",
	            $start, $tab-length($line)-11, " ",  $line;
	    } else {
	        printf FH "            new char[] {'\\x%04s', '\\x0000'},%*s/* %s */\n",
        	    $start, $tab-length($line)-51, " ",  $line;
	    }
	}
    } else {
	$intable = 1 if !$intable;
	$tablename = $1;

	($varname = $tablename) =~ tr/./_/;
	$header =~ s/\n/\n        \/\/\/ /s;

	print FH "\n        /// <summary>\n        /// $header        /// </summary>\n";
	if ($varname =~ /^B/) {
	    print FH "        public static readonly string[] ${varname} = new string[]\n        {\n";
	} else {
	    print FH "        public static readonly char[][] ${varname} = new char[][]\n        {\n";
	}
	
    }
}

print FH <<EOF;
    }
}
EOF
close FH or die "cannot close $filename";

unlink "temp.dll";
unlink "$profile.resx";
system "csc /out:temp.dll /t:library $filename" and die;
system "../ResTool/bin/Debug/ResTool.exe temp.dll $profile.resx";
unlink "temp.dll";
#unlink $filename;
