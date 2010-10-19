<?php
# Copyright (C) 2004 Brion Vibber <brion@pobox.com>
# http://www.mediawiki.org/
# 
# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2 of the License, or 
# (at your option) any later version.
# 
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
# GNU General Public License for more details.
# 
# You should have received a copy of the GNU General Public License along
# with this program; if not, write to the Free Software Foundation, Inc.,
# 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
# http://www.gnu.org/copyleft/gpl.html

/**
 * Some of these functions are adapted from places in MediaWiki.
 * Should probably merge them for consistency.
 *
 * @package UtfNormal
 * @access public
 */

/** */

/**
 * Return UTF-8 sequence for a given Unicode code point.
 * May die if fed out of range data.
 *
 * @param int $codepoint
 * @return string
 * @access public
 */
function codepointToUtf8( $codepoint ) {
	if($codepoint <		0x80) return chr($codepoint);
	if($codepoint <    0x800) return chr($codepoint >>	6 & 0x3f | 0xc0) .
									 chr($codepoint		  & 0x3f | 0x80);
	if($codepoint <  0x10000) return chr($codepoint >> 12 & 0x0f | 0xe0) .
									 chr($codepoint >>	6 & 0x3f | 0x80) .
									 chr($codepoint		  & 0x3f | 0x80);
	if($codepoint < 0x110000) return chr($codepoint >> 18 & 0x07 | 0xf0) .
									 chr($codepoint >> 12 & 0x3f | 0x80) .
									 chr($codepoint >>	6 & 0x3f | 0x80) .
									 chr($codepoint		  & 0x3f | 0x80);

	die("Asked for code outside of range ($codepoint)\n");
}

/**
 * Take a series of space-separated hexadecimal numbers representing
 * Unicode code points and return a UTF-8 string composed of those
 * characters. Used by UTF-8 data generation and testing routines.
 *
 * @param string $sequence
 * @return string
 * @access private
 */
function hexSequenceToUtf8( $sequence ) {
	$utf = '';
	foreach( explode( ' ', $sequence ) as $hex ) {
		$n = hexdec( $hex );
		$utf .= codepointToUtf8( $n );
	}
	return $utf;
}

/**
 * Take a UTF-8 string and return a space-separated series of hex
 * numbers representing Unicode code points. For debugging.
 *
 * @param string $str
 * @return string
 * @access private
 */
function utf8ToHexSequence( $str ) {
	return rtrim( preg_replace( '/(.)/uSe',
	                            'sprintf("%04x ", utf8ToCodepoint("$1"))',
	                            $str ) );
}

/**
 * Determine the Unicode codepoint of a single-character UTF-8 sequence.
 * Does not check for invalid input data.
 *
 * @param string $char
 * @return int
 * @access public
 */
function utf8ToCodepoint( $char ) {
	# Find the length
	$z = ord( $char{0} );
	if ( $z & 0x80 ) {
		$length = 0;
		while ( $z & 0x80 ) {
			$length++;
			$z <<= 1;
		}
	} else {
		$length = 1;
	}

	if ( $length != strlen( $char ) ) {
		return false;
	}
	if ( $length == 1 ) {
		return ord( $char );
	}

	# Mask off the length-determining bits and shift back to the original location
	$z &= 0xff;
	$z >>= $length;

	# Add in the free bits from subsequent bytes
	for ( $i=1; $i<$length; $i++ ) {
		$z <<= 6;
		$z |= ord( $char{$i} ) & 0x3f;
	}

	return $z;
}

/**
 * Escape a string for inclusion in a PHP single-quoted string literal.
 *
 * @param string $string
 * @return string
 * @access public
 */
function escapeSingleString( $string ) {
	return strtr( $string,
		array(
			'\\' => '\\\\',
			'\'' => '\\\''
		));
}

?>
