/**
 * gettext-cs-utils
 *
 * Copyright 2011 Manas Technology Solutions 
 * http://www.manas.com.ar/
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either 
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public 
 * License along with this library.  If not, see <http://www.gnu.org/licenses/>.
 * 
 **/
 
 
/**************************************************************************
/* Getopt.cs -- C#.NET port of Java port of GNU getopt from glibc 2.0.6
/*
/* Copyright (c) 1987-1997 Free Software Foundation, Inc.
/* Java Port Copyright (c) 1998 by Aaron M. Renn (arenn@urbanophile.com)
/* C#.NET Port Copyright (c) 2004 by Klaus Prückl (klaus.prueckl@aon.at)
/*
/* This program is free software; you can redistribute it and/or modify
/* it under the terms of the GNU Library General Public License as published 
/* by  the Free Software Foundation; either version 2 of the License or
/* (at your option) any later version.
/*
/* This program is distributed in the hope that it will be useful, but
/* WITHOUT ANY WARRANTY; without even the implied warranty of
/* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
/* GNU Library General Public License for more details.
/*
/* You should have received a copy of the GNU Library General Public License
/* along with this program; see the file COPYING.LIB.  If not, write to 
/* the Free Software Foundation Inc., 59 Temple Place - Suite 330, 
/* Boston, MA  02111-1307 USA
/**************************************************************************/

using System;
using System.Configuration;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Gnu.Getopt
{
	/// <summary>
	/// This is a C# port of a Java port of GNU getopt, a class for parsing
	/// command line arguments passed to programs. It it based on the C
	/// getopt() functions in glibc 2.0.6 and should parse options in a 100%
	/// compatible manner. If it does not, that is a bug. The programmer's
	/// interface is also very compatible.
	/// </summary>
	/// <remarks>
	/// To use Getopt, create a Getopt object with a args array passed to the
	/// main method, then call the <see cref="getopt"/> method in a loop. It
	/// will return an <see cref="int"/> that contains the value of the option
	/// character parsed from the command line. When there are no more options
	/// to be parsed, it returns -1.
	/// <para>
	/// A command line option can be defined to take an argument. If an option
	/// has an argument, the value of that argument is stored in an instance
	/// variable called <c>optarg</c>, which can be accessed using the
	/// <see cref="Optarg"/> property.
	/// If an option that requires an argument is found, but there is no
	/// argument present, then an error message is printed. Normally
	/// <see cref="getopt"/> returns a '<c>?</c>' in this situation, but that
	/// can be changed as described below.
	/// </para>
	/// <para>
	/// If an invalid option is encountered, an error message is printed to
	/// the standard error and <see cref="getopt"/> returns a '<c>?</c>'.
	/// The value of the invalid option encountered is stored in the instance
	/// variable optopt which can be retrieved using the <see cref="Optopt"/>
	/// property.
	/// To suppress the printing of error messages for this or any other error,
	/// set the value of the <c>opterr</c> instance variable to false using the
	/// <see cref="Opterr"/> property.
	/// </para>
	/// <para>
	/// Between calls to <see cref="getopt"/>, the instance variable
	/// <c>optind</c> is used to keep track of where the object is in the
	/// parsing process. After all options have been returned, <c>optind</c>
	/// is the index in argv of the first non-option argument.
	/// This variable can be accessed with the <see cref="Optind"/> property.
	/// </para>
	/// <para>
	/// Note that this object expects command line options to be passed in the
	/// traditional Unix manner. That is, proceeded by a '<c>-</c>' character.
	/// Multiple options can follow the '<c>-</c>'. For example "<c>-abc</c>"
	/// is equivalent to "<c>-a -b -c</c>". If an option takes a required
	/// argument, the value of the argument can immediately follow the option
	/// character or be present in the next argv element. For example,
	/// "<c>-cfoo</c>" and "<c>-c foo</c>" both represent an option character
	/// of '<c>c</c>' with an argument of "<c>foo</c>" assuming <c>c</c> takes
	/// a required argument.
	/// If an option takes an argument that is not required, then any argument
	/// must immediately follow the option character in the same argv element.
	/// For example, if c takes a non-required argument, then "<c>-cfoo</c>"
	/// represents option character '<c>c</c>' with an argument of "<c>foo</c>"
	/// while "<c>-c foo</c>" represents the option character '<c>c</c>' with
	/// no argument, and a first non-option argv element of "<c>foo</c>".
	/// </para>
	/// <para>
	/// The user can stop <see cref="getopt"/> from scanning any further into
	/// a command line by using the special argument "<c>--</c>" by itself.
	/// For example:
	/// "<c>-a -- -d</c>" would return an option character of '<c>a</c>', then
	/// return -1.
	/// The "<c>--</c>" is discarded and "<c>-d</c>" is pointed to by
	/// <c>optind</c> as the first non-option argv element.
	/// </para>
	/// <example>
	/// Here is a basic example of using Getopt:
	/// <code>
	/// Getopt g = new Getopt("testprog", args, "ab:c::d");
	///	
	///	int c;
	///	string arg;
	///	while ((c = g.getopt()) != -1)
	///	{
	///		switch(c)
	///		{
	///			case 'a':
	///			case 'd':
	///				Console.WriteLine("You picked " + (char)c );
	///				break;
	///					
	///			case 'b':
	///			case 'c':
	///				arg = g.Optarg;
	///				Console.WriteLine("You picked " + (char)c + 
	///					" with an argument of " +
	///					((arg != null) ? arg : "null") );
	///				break;
	///	
	///			case '?':
	///				break; // getopt() already printed an error
	///	
	///			default:
	///				Console.WriteLine("getopt() returned " + c);
	///				break;
	///		}
	///	}
	/// </code>
	/// In this example, a new Getopt object is created with three params. The
	/// first param is the program name. This is for printing error messages in
	/// the form "program: error message". In the C version, this value is
	/// taken from argv[0], but in .NET the program name is not passed in that
	/// element, thus the need for this parameter. The second param is the
	/// argument list that was passed to the main() method. The third param is
	/// the list of valid options. Each character represents a valid option. If
	/// the character is followed by a single colon, then that option has a
	/// required argument. If the character is followed by two colons, then
	/// that option has an argument that is not required.
	/// <para>
	/// Note in this example that the value returned from <see cref="getopt"/>
	/// is cast to a char prior to printing. This is required in order to make
	/// the value display correctly as a character instead of an integer.
	/// </para>
	/// </example>
	/// If the first character in the option string is a colon, for example
	/// "<c>:abc::d</c>", then <see cref="getopt"/> will return a '<c>:</c>'
	/// instead of a '<c>?</c>' when it encounters an option with a missing
	/// required argument. This allows the caller to distinguish between
	/// invalid options and valid options that are simply incomplete.
	/// <para>
	/// In the traditional Unix getopt(), -1 is returned when the first
	/// non-option charcter is encountered. In GNU getopt(), the default
	/// behavior is to allow options to appear anywhere on the command line.
	/// The <see cref="getopt"/> method permutes the argument to make it appear
	/// to the caller that all options were at the beginning of the command
	/// line, and all non-options were at the end. For example, calling
	/// <see cref="getopt"/> with command line argv of "<c>-a foo bar -d</c>"
	/// returns options '<c>a</c>' and '<c>d</c>', then sets optind to point to
	/// "<c>foo</c>". The program would read the last two argv elements as
	/// "<c>foo</c>" and "<c>bar</c>", just as if the user had typed
	/// "<c>-a -d foo bar</c>". 
	/// </para>
	/// <para> 
	/// The user can force <see cref="getopt"/> to stop scanning the command
	/// line with the special argument "<c>--</c>" by itself. Any elements
	/// occuring before the "<c>--</c>" are scanned and permuted as normal. Any
	/// elements after the "<c>--</c>" are returned as is as non-option argv
	/// elements. For example, "<c>foo -a -- bar -d</c>" would return option
	/// '<c>a</c>' then -1. <c>optind</c> would point  to "<c>foo</c>",
	/// "<c>bar</c>" and "<c>-d</c>" as the non-option argv elements. The
	/// "<c>--</c>" is discarded by <see cref="getopt"/>.
	/// </para>
	/// <para>
	/// There are two ways this default behavior can be modified. The first is
	/// to specify traditional Unix getopt() behavior (which is also POSIX
	/// behavior) in which scanning stops when the first non-option argument
	/// encountered. (Thus "<c>-a foo bar -d</c>" would return '<c>a</c>' as an
	/// option and have "<c>foo</c>", "<c>bar</c>", and "<c>-d</c>" as
	/// non-option elements).
	/// The second is to allow options anywhere, but to return all elements in
	/// the order they occur on the command line.
	/// When a non-option element is ecountered, an integer 1 is returned and
	/// the value of the non-option element is stored in <c>optarg</c> is if
	/// it were the argument to that option.
	/// For example, "<c>-a foo -d</c>", returns first '<c>a</c>', then 1 (with
	/// <c>optarg</c> set to "<c>foo</c>") then '<c>d</c>' then -1.
	/// When this "return in order" functionality is enabled, the only way to
	/// stop <c>getopt</c> from scanning all command line elements is to
	/// use the special "<c>--</c>" string by itself as described above. An
	/// example is "<c>-a foo -b -- bar</c>", which would return '<c>a</c>',
	/// then integer 1 with <c>optarg</c> set to "<c>foo</c>", then '<c>b</c>',
	/// then -1.
	/// <c>optind</c> would then point to "<c>bar</c>" as the first non-option
	/// argv element. The "<c>--</c>" is discarded.
	/// </para>
	/// <para>
	/// The POSIX/traditional behavior is enabled by either setting the 
	/// application setting "Gnu.PosixlyCorrect" or by putting a '<c>+</c>'
	/// sign as the first character of the option string.
	/// The difference between the two methods is that setting the
	/// "Gnu.PosixlyCorrect" application setting also forces certain error
	/// messages to be displayed in POSIX format.
	/// To enable the "return in order" functionality, put a '<c>-</c>' as the
	/// first character of the option string. Note that after determining the
	/// proper behavior, Getopt strips this leading '<c>+</c>' or '<c>-</c>',
	/// meaning that a '<c>:</c>' placed as the second character after one of
	/// those two will still cause <see cref="getopt"/> to return a '<c>:</c>'
	/// instead of a '<c>?</c>' if a required option argument is missing.
	/// </para>
	/// <para>
	/// In addition to traditional single character options, GNU Getopt also
	/// supports long options. These are preceeded by a "<c>--</c>" sequence
	/// and can be as long as desired. Long options provide a more
	/// user-friendly way of entering command line options.
	/// For example, in addition to a "<c>-h</c>" for help, a program could
	/// support also "<c>--help</c>".
	/// </para>
	/// <para>
	/// Like short options, long options can also take a required or
	/// non-required argument. Required arguments can either be specified by
	/// placing an equals sign after the option name, then the argument, or by
	/// putting the argument in the next argv element. For example:
	/// "<c>--outputdir=foo</c>" and "<c>--outputdir foo</c>" both represent an
	/// option of "<c>outputdir</c>" with an argument of "<c>foo</c>", assuming
	/// that outputdir takes a required argument. If a long option takes a
	/// non-required argument, then the equals sign form must be used to
	/// specify the argument. In this case, "<c>--outputdir=foo</c>" would
	/// represent option outputdir with an argument of <c>foo</c> while
	/// "<c>--outputdir foo</c>" would represent the option outputdir with no
	/// argument and a first non-option argv element of "<c>foo</c>".
	/// </para>
	/// <para>
	/// Long options can also be specified using a special POSIX argument
	/// format (one that I highly discourage). This form of entry is enabled by
	/// placing a "<c>W;</c>" (yes, '<c>W</c>' then a semi-colon) in the valid
	/// option string.
	/// This causes getopt to treat the name following the "<c>-W</c>" as the
	/// name of the long option. For example, "<c>-W outputdir=foo</c>" would
	/// be equivalent to "<c>--outputdir=foo</c>". The name can immediately
	/// follow the "<c>-W</c>" like so: "<c>-Woutputdir=foo</c>".
	/// Option arguments are handled identically to normal long options. If a
	/// string follows the "<c>-W</c>" that does not represent a
	/// valid long option, then <see cref="getopt"/> returns '<c>W</c>' and
	/// the caller must decide what to do. Otherwise <see cref="getopt"/>
	/// returns a long option value as described below.
	/// </para>
	/// <para>
	/// While long options offer convenience, they can also be tedious to type
	/// in full. So it is permissible to abbreviate the option name to as few
	/// characters as required to uniquely identify it. If the name can
	/// represent multiple long options, then an error message is printed and
	/// <see cref="getopt"/> returns a '<c>?</c>'.  
	/// </para>
	/// <para>
	/// If an invalid option is specified or a required option argument is 
	/// missing, <see cref="getopt"/> prints an error and returns a '<c>?</c>'
	/// or '<c>:</c>' exactly as for short options.
	/// Note that when an invalid long option is encountered, the <c>optopt</c>
	/// variable is set to integer 0 and so cannot be used to identify the
	/// incorrect option the user entered.
	/// </para>
	/// <para>
	/// Long options are defined by <see cref="LongOpt"/> objects. These
	/// objects are created with a contructor that takes four params: a string
	/// representing the object name, a integer specifying what arguments the
	/// option takes (the value is one of the <see cref="Argument"/>
	/// enumeration: <see cref="Argument.No"/>,
	/// <see cref="Argument.Required"/>, or <see cref="Argument.Optional"/>),
	/// a <see cref="System.Text.StringBuilder"/> flag object (described
	/// below), and an integer value (described below).
	/// </para>
	/// <para>
	/// To enable long option parsing, create an array of
	/// <see cref="LongOpt"/>'s representing the legal options and pass it to
	/// the Getopt() constructor.
	/// WARNING: If all elements of the array are not populated with
	/// <see cref="LongOpt"/> objects, the <see cref="getopt"/> method will
	/// throw a <see cref="NullReferenceException"/>.
	/// </para>
	/// <para>
	/// When <see cref="getopt"/> is called and a long option is encountered,
	/// one of two things can be returned.
	/// If the flag field in the <see cref="LongOpt"/> object representing the
	/// long option is non-null, then the integer value field is stored there
	/// and an integer 0 is returned to the caller.
	/// The <c>val</c> field can then be retrieved from the <c>flag</c> field.
	/// Note that since the <c>flag</c> field is a
	/// <see cref="System.Text.StringBuilder"/>, the appropriate string to
	/// integer converions must be performed in order to get the actual int
	/// value stored there.
	/// If the <c>flag</c> field in the <see cref="LongOpt"/> object is null,
	/// then the value field of the <see cref="LongOpt"/> is returned.
	/// This can be the character of a short option.
	/// This allows an app to have both a long and short option sequence (say,
	/// "<c>-h</c>" and "<c>--help</c>") that do the exact same thing.
	/// </para>
	/// <para>
	/// With long options, there is an alternative method of determining which
	/// option was selected. The property Longind will return the index in the
	/// long option array (NOT argv) of the long option found. So if multiple
	/// long options are configured to return the same value, the application
	/// can use <see cref="Longind"/> to distinguish between them. 
	/// </para>
	/// <example>
	/// Here is an expanded Getopt example using long options and various
	/// techniques described above:
	/// <code>
	/// int c;
	/// string arg;
	/// LongOpt[] longopts = new LongOpt[3];
	/// 
	/// StringBuffer sb = new StringBuffer();
	/// longopts[0] = new LongOpt("help", LongOpt.NO_ARGUMENT, null, 'h');
	/// longopts[1] = new LongOpt("outputdir", LongOpt.REQUIRED_ARGUMENT, sb, 'o'); 
	/// longopts[2] = new LongOpt("maximum", LongOpt.OPTIONAL_ARGUMENT, null, 2);
	/// 
	/// Getopt g = new Getopt("testprog", argv, "-:bc::d:hW;", longopts);
	/// g.Opterr = false; // We'll do our own error handling
	/// 
	/// while ((c = g.getopt()) != -1)
	///		switch (c)
	///		{
	///			case 0:
	///				arg = g.getOptarg();
	///				Console.WriteLine("Got long option with value '" +
	///					(char)(new Integer(sb.toString())).intValue()
	///					+ "' with argument " +
	///					((arg != null) ? arg : "null"));
	///				break;
	///	
	///			case 1:
	///				Console.WriteLine("I see you have return in order set and that " +
	///					"a non-option argv element was just found " +
	///					"with the value '" + g.Optarg + "'");
	///				break;
	/// 
	///			case 2:
	///				arg = g.getOptarg();
	///				Console.WriteLine("I know this, but pretend I didn't");
	///				Console.WriteLine("We picked option " +
	///					longopts[g.Longind].getName() +
	///					" with value " + 
	///					((arg != null) ? arg : "null"));
	///				break;
	///	
	///			case 'b':
	///				Console.WriteLine("You picked plain old option " + (char)c);
	///				break;
	///	
	///			case 'c':
	///			case 'd':
	///				arg = g.getOptarg();
	///				Console.WriteLine("You picked option '" + (char)c + 
	///					"' with argument " +
	///					((arg != null) ? arg : "null"));
	///				break;
	///	
	///			case 'h':
	///				Console.WriteLine("I see you asked for help");
	///				break;
	///	
	///			case 'W':
	///				Console.WriteLine("Hmmm. You tried a -W with an incorrect long " +
	///					"option name");
	///				break;
	///	
	///			case ':':
	///				Console.WriteLine("Doh! You need an argument for option " +
	///					(char)g.getOptopt());
	///				break;
	///	
	///			case '?':
	///				Console.WriteLine("The option '" + (char)g.getOptopt() + 
	///					"' is not valid");
	///				break;
	///	
	///			default:
	///				Console.WriteLine("getopt() returned " + c);
	///				break;
	///		}
	///	
	/// for (int i = g.getOptind(); i &lt; argv.length ; i++)
	///		Console.WriteLine("Non option argv element: " + argv[i] );
	/// </code>
	/// <para>
	/// There is an alternative form of the constructor used for long options
	/// above. This takes a trailing boolean flag. If set to false, Getopt
	/// performs identically to the example, but if the boolean flag is true
	/// then long options are allowed to start with a single '<c>-</c>' instead
	/// of "<c>--</c>". If the first character of the option is a valid short
	/// option character, then the option is treated as if it were the short
	/// option. Otherwise it behaves as if the option is a long option.
	/// Note that the name given to this option - <c>longOnly</c> - is very
	/// counter-intuitive.
	/// It does not cause only long options to be parsed but instead enables
	/// the behavior described above.
	/// </para>
	/// </example>
	/// <para> 
	/// Note that the functionality and variable names used are driven from the
	/// C lib version as this object is a port of the C code, not a new
	/// implementation. This should aid in porting existing C/C++ code, as well
	/// as helping programmers familiar with the glibc version to adapt to the
	/// C#.NET version.
	/// </para>
	/// </remarks>
	/// <author>Roland McGrath (roland@gnu.ai.mit.edu)</author>
	/// <author>Ulrich Drepper (drepper@cygnus.com)</author>
	/// <author>Aaron M. Renn (arenn@urbanophile.com)</author>
	/// <author>Klaus Prückl (klaus.prueckl@aon.at)</author>
	/// <seealso cref="LongOpt">LongOpt</seealso>
	public class Getopt
	{
		/// <summary>
		/// Describe how to deal with options that follow non-option
		/// ARGV-elements.
		///
		/// If the caller did not specify anything, the default is RequireOrder
		/// if the application setting Gnu.PosixlyCorrect is defined, Permute
		/// otherwise.
		///
		/// The special argument "<c>--</c>" forces an end of option-scanning
		/// regardless of the value of <c>ordering</c>. In the case of
		/// ReturnInOrder, only <c>--</c> can cause <see cref="getopt"/> to
		/// return -1 with <c>optind</c> != argv.Length.
		/// </summary>
		private enum Order 
		{
			/// <summary>
			/// RequireOrder means don't recognize them as options; stop option
			/// processing when the first non-option is seen. This is what Unix
			/// does. This mode of operation is selected by either setting the
			/// application setting Gnu.PosixlyCorrect, or using '<c>+</c>' as
			/// the first character of the list of option characters.
			/// </summary>
			RequireOrder = 1,
			/// <summary>
			/// Permute is the default. We permute the contents of ARGV as we
			/// scan, so that eventually all the non-options are at the end.
			/// This allows options to be given in any order, even with
			/// programs that were not written to expect this.
			/// </summary>
			Permute = 2,
			/// <summary>
			/// ReturnInOrder is an option available to programs that were
			/// written to expect options and other ARGV-elements in any order
			/// and that care about the ordering of the two. We describe each
			/// non-option ARGV-element as if it were the argument of an option
			/// with character code 1. Using '<c>-</c>' as the first character
			/// of the list of option characters selects this mode of
			/// operation.
			/// </summary>
			ReturnInOrder = 3
		}
		
		#region Instance Variables
		/// <summary>
		/// For communication from <see cref="getopt"/> to the caller. When
		/// <see cref="getopt"/> finds an option that takes an argument, the
		/// argument value is returned here. Also, when <c>ordering</c> is
		/// <see cref="Order.ReturnInOrder"/>, each non-option ARGV-element is
		/// returned here.
		/// </summary>
		private string optarg;
		
		/// <summary>
		/// Index in ARGV of the next element to be scanned. This is used for
		/// communication to and from the caller and for communication between
		/// successive calls to <see cref="getopt"/>.
		///
		/// On entry to <see cref="getopt"/>, zero means this is the first
		/// call; initialize.
		///
		/// When <see cref="getopt"/> returns -1, this is the index of the
		/// first of the non-option elements that the caller should itself
		/// scan.
		///
		/// Otherwise, <see cref="getopt"/> communicates from one call to the
		/// next how much of ARGV has been scanned so far.
		/// </summary>
		private int optind = 0;
		
		/// <summary>
		/// Callers store false here to inhibit the error message for
		/// unrecognized options.
		/// </summary>
		private bool opterr = true;
		
		/// <summary>
		/// When an unrecognized option is encountered, getopt will return a
		/// '<c>?</c>' and store the value of the invalid option here.
		/// </summary>
		private int optopt = '?';
		
		/// <summary>
		/// The next char to be scanned in the option-element in which the last
		/// option character we returned was found. This allows us to pick up
		/// the scan where we left off.
		///
		/// If this is zero, or a null string, it means resume the scan by
		/// advancing to the next ARGV-element.  
		/// </summary>
		private string nextchar;
		
		/// <summary>
		/// This is the string describing the valid short options.
		/// </summary>
		private string optstring;
		
		/// <summary>
		/// This is an array of <see cref="LongOpt"/> objects which describ the
		/// valid long options.
		/// </summary>
		private LongOpt[] longOptions;
		
		/// <summary>
		/// This flag determines whether or not we are parsing only long args.
		/// </summary>
		private bool longOnly;
		
		/// <summary>
		/// Stores the index into the <c>longOptions</c> array of the long
		/// option found.
		/// </summary>
		private int longind;
		
		/// <summary>
		/// The flag determines whether or not we operate in strict POSIX
		/// compliance.
		/// </summary>
		private bool posixlyCorrect;
		
		/// <summary>
		/// A flag which communicates whether or not
		/// <see cref="checkLongOption"/> did all necessary processing for the
		/// current option.
		/// </summary>
		private bool longoptHandled;
		
		/// <summary>
		/// The index of the first non-option in argv[].
		/// </summary>
		private int firstNonopt = 1;
		
		/// <summary>
		/// The index of the last non-option in argv[].
		/// </summary>
		private int lastNonopt = 1;
		
		/// <summary>
		/// Flag to tell <see cref="getopt"/> to immediately return -1 the next
		/// time it is called.
		/// </summary>
		private bool endparse = false;
		
		/// <summary>
		/// Saved argument list passed to the program.
		/// </summary>
		private string[] argv;
		
		/// <summary>
		/// Determines whether we permute arguments or not.
		/// </summary>
		private Order ordering;
		
		/// <summary>
		/// Name to print as the program name in error messages. This is
		/// necessary since .NET does not place the program name in args[0].
		/// </summary>
		private string progname;
		
		/// <summary>
		/// The localized strings are kept in the resources, which can be
		/// accessed by the <see cref="ResourceManager"/> class.
		/// </summary>
		private ResourceManager resManager = new ResourceManager(
			"Gnu.Getopt.MessagesBundle", Assembly.GetExecutingAssembly());
		
		/// <summary>
		/// The current UI culture (set to en-US when posixly correctness is
		/// enabled).
		/// </summary>
		private CultureInfo cultureInfo = CultureInfo.CurrentUICulture;

		#endregion
	
		#region Constructors
		/// <summary>
		/// Construct a basic Getopt instance with the given input data. Note
		/// that this handles short options only.
		/// </summary>
		/// <param name="progname">
		/// The name to display as the program name when printing errors.
		/// </param>
		/// <param name="argv">
		/// The string array passed as the command line to the program.
		/// </param>
		/// <param name="optstring">
		/// A string containing a description of the valid args for this
		/// program.
		/// </param>
		public Getopt(string progname, string[] argv, string optstring) :
			this(progname, argv, optstring, null, false)
		{
		}
		
		/// <summary>
		/// Construct a Getopt instance with given input data that is capable
		/// of parsing long options as well as short.
		/// </summary>
		/// <param name="progname">
		/// The name to display as the program name when printing errors.
		/// </param>
		/// <param name="argv">
		/// The string array passed as the command ilne to the program.
		/// </param>
		/// <param name="optstring">
		/// A string containing a description of the valid short args for this
		/// program.
		/// </param>
		/// <param name="longOptions">
		/// An array of <see cref="LongOpt"/> objects that describes the valid
		/// long args for this program.
		/// </param>
		public Getopt(string progname, string[] argv, string optstring,
			LongOpt[] longOptions) : this(progname, argv, optstring,
			longOptions, false)
		{
		}
		
		/// <summary>
		/// Construct a Getopt instance with given input data that is capable
		/// of parsing long options and short options.  Contrary to what you
		/// might think, the flag <paramref name="longOnly"/> does not
		/// determine whether or not we scan for only long arguments. Instead,
		/// a value of true here allows long arguments to start with a
		/// '<c>-</c>' instead of "<c>--</c>" unless there is a conflict with
		/// a short option name.
		/// </summary>
		/// <param name="progname">
		/// The name to display as the program name when printing errors
		/// </param>
		/// <param name="argv">
		/// The string array passed as the command ilne to the program.
		/// </param>
		/// <param name="optstring">
		/// A string containing a description of the valid short args for this
		/// program.
		/// </param>
		/// <param name="longOptions">
		/// An array of <see cref="LongOpt"/> objects that describes the valid
		/// long args for this program.
		/// </param>
		/// <param name="longOnly">
		/// true if long options that do not conflict with short options can
		/// start with a '<c>-</c>' as well as "<c>--</c>".
		/// </param>
		public Getopt(string progname, string[] argv, string optstring,
			LongOpt[] longOptions, bool longOnly)
		{
			if (optstring.Length == 0)
				optstring = " ";
			
			// This function is essentially _getopt_initialize from GNU getopt
			this.progname = progname;
			this.argv = argv;
			this.optstring = optstring;
			this.longOptions = longOptions;
			this.longOnly = longOnly;
			
			// Check for application setting "Gnu.PosixlyCorrect" to determine
			// whether to strictly follow the POSIX standard. This replaces the
			// "POSIXLY_CORRECT" environment variable in the C version
			try 
			{
				if((bool) new AppSettingsReader().GetValue(
					"Gnu.PosixlyCorrect", typeof(bool))) 
				{
					this.posixlyCorrect = true;
					this.cultureInfo = new CultureInfo("en-US");
				}
				else
					this.posixlyCorrect = false;
			}
			catch(Exception) 
			{
				this.posixlyCorrect = false;
			}
			
			// Determine how to handle the ordering of options and non-options
			if (optstring[0] == '-')
			{
				this.ordering = Order.ReturnInOrder;
				if (optstring.Length > 1)
					this.optstring = optstring.Substring(1);
			}
			else if (optstring[0] == '+')
			{
				this.ordering = Order.RequireOrder;
				if (optstring.Length > 1)
					this.optstring = optstring.Substring(1);
			}
			else if (this.posixlyCorrect)
			{
				this.ordering = Order.RequireOrder;
			}
			else
			{
				this.ordering = Order.Permute; // The normal default case
			}
		}
		#endregion
		
		#region Instance Methods
		/// <summary>
		/// In GNU getopt, it is possible to change the string containg valid
		/// options on the fly because it is passed as an argument to
		/// <see cref="getopt"/> each time.
		/// In this version we do not pass the string on every call.
		/// In order to allow dynamic option string changing, this method is
		/// provided.
		/// </summary>
		public string Optstring
		{
			get { return this.optstring; }
			set
			{
				if (value.Length == 0)
					value = " ";
				
				this.optstring = value;
			}
		}

		/// <summary>
		/// <c>optind</c> is the index in ARGV of the next element to be
		/// scanned. This is used for communication to and from the caller and
		/// for communication between successive calls to <see cref="getopt"/>.
		///
		/// When <see cref="getopt"/> returns -1, this is the index of the
		/// first of the non-option elements that the caller should itself
		/// scan.
		///
		/// Otherwise, <c>optind</c> communicates from one call to the next how
		/// much of ARGV has been scanned so far.  
		/// </summary>
		/// <summary>
		/// This method allows the <c>optind</c> index to be set manually.
		/// Normally this is not necessary (and incorrect usage of this method
		/// can lead to serious lossage), but <c>optind</c> is a public symbol
		/// in GNU getopt, so this method was added to allow it to be modified
		/// by the caller if desired.
		/// </summary>
		public int Optind
		{
			get	{ return this.optind; }
			set	{ this.optind = value; }
		}

		/// <summary>
		/// Since in GNU getopt() the argument vector is passed back in to the
		/// function every time, the caller can swap out <c>argv</c> on the
		/// fly. Since passing argv is not required in the .NET version, this
		/// method allows the user to override argv. Note that incorrect use of
		/// this method can lead to serious lossage.
		/// </summary>
		public string[] Argv
		{
			get { return this.argv; }
			set { this.argv = value; }
		}

		/// <summary>
		/// For communication from <see cref="getopt"/> to the caller. When
		/// <see cref="getopt"/> finds an option that takes an argument, the
		/// argument value is returned here. Also, when <c>ordering</c> is
		/// <see cref="Order.ReturnInOrder"/>, each non-option ARGV-element is
		/// returned here. No set method is provided because setting this
		/// variable has no effect.
		/// </summary>
		public string Optarg
		{
			get	{ return this.optarg; }
		}

		/// <summary>
		/// Normally <see cref="Getopt"/> will print a message to the standard
		/// error when an invalid option is encountered. This can be suppressed
		/// (or re-enabled) by calling this method.
		/// </summary>
		public bool Opterr
		{
			get { return this.opterr; }
			set	{ this.opterr = value; }
		}

		/// <summary>
		/// When <see cref="getopt"/> encounters an invalid option, it stores
		/// the value of that option in <c>optopt</c> which can be retrieved
		/// with this method. There is no corresponding set method because
		/// setting this variable has no effect.
		/// </summary>
		public int Optopt
		{
			get	{ return this.optopt; }
		}

		/// <summary>
		/// Returns the index into the array of long options (NOT argv)
		/// representing the long option that was found.
		/// </summary>
		public int Longind
		{
			get	{ return this.longind; }
		}
		
		/// <summary>
		/// Exchange the shorter segment with the far end of the longer
		/// segment. That puts the shorter segment into the right place. It
		/// leaves the longer segment in the right place overall, but it
		/// consists of two parts that need to be swapped next. This method is
		/// used by <see cref="getopt"/> for argument permutation.
		/// </summary>
		private void exchange(string[] argv)
		{
			int bottom = this.firstNonopt;
			int middle = this.lastNonopt;
			int top = this.optind;
			string tem;
			
			while (top > middle && middle > bottom)
			{
				if (top - middle > middle - bottom)
				{
					// Bottom segment is the short one. 
					int len = middle - bottom;
					int i;
					
					// Swap it with the top part of the top segment. 
					for (i = 0; i < len; i++)
					{
						tem = argv[bottom + i];
						argv[bottom + i] = argv[top - (middle - bottom) + i];
						argv[top - (middle - bottom) + i] = tem;
					}
					// Exclude the moved bottom segment from further swapping.
					top -= len;
				}
				else
				{
					// Top segment is the short one.
					int len = top - middle;
					int i;
					
					// Swap it with the bottom part of the bottom segment. 
					for (i = 0; i < len; i++)
					{
						tem = argv[bottom + i];
						argv[bottom + i] = argv[middle + i];
						argv[middle + i] = tem;
					}
					// Exclude the moved top segment from further swapping. 
					bottom += len;
				}
			}
			
			// Update records for the slots the non-options now occupy. 
			
			this.firstNonopt += (this.optind - this.lastNonopt);
			this.lastNonopt = this.optind;
		}
		
		/// <summary>
		/// Check to see if an option is a valid long option. Called by
		/// <see cref="getopt"/>. Put in a separate method because this needs
		/// to be done twice. (The C getopt authors just copy-pasted the
		/// code!).
		/// </summary>
		/// <returns>
		/// Various things depending on circumstances
		/// </returns>
		private int checkLongOption()
		{
			LongOpt pfound = null;
			int nameend;
			bool ambig;
			bool exact;
			
			this.longoptHandled = true;
			ambig = false;
			exact = false;
			this.longind = - 1;
			
			nameend = this.nextchar.IndexOf("=");
			if (nameend == - 1)
				nameend = this.nextchar.Length;
			
			// Test all long options for either exact match or abbreviated
			// matches
			for (int i = 0; i < this.longOptions.Length; i++)
			{
				if (this.longOptions[i].Name.StartsWith(
					this.nextchar.Substring(0, nameend)))
				{
					if (this.longOptions[i].Name.Equals(
						this.nextchar.Substring(0, nameend)))
					{
						// Exact match found
						pfound = this.longOptions[i];
						this.longind = i;
						exact = true;
						break;
					}
					else if (pfound == null)
					{
						// First nonexact match found
						pfound = this.longOptions[i];
						this.longind = i;
					}
					else
					{
						// Second or later nonexact match found
						ambig = true;
					}
				}
			} // for
			
			// Print out an error if the option specified was ambiguous
			if (ambig && !exact)
			{
				if (this.opterr)
				{
					object[] msgArgs = new object[]{
						this.progname, this.argv[optind] };
					System.Console.Error.WriteLine(
						this.resManager.GetString("getopt.ambigious",
						this.cultureInfo), msgArgs);
				}
				
				this.nextchar = "";
				this.optopt = 0;
				++this.optind;
				
				return '?';
			}
			
			if (pfound != null)
			{
				++this.optind;
				
				if (nameend != this.nextchar.Length)
				{
					if (pfound.HasArg != Argument.No)
					{
						if (this.nextchar.Substring(nameend).Length > 1)
							this.optarg = this.nextchar.Substring(nameend + 1);
						else
							this.optarg = "";
					}
					else
					{
						if (this.opterr)
						{
							// -- option
							if (argv[this.optind - 1].StartsWith("--"))
							{
								object[] msgArgs = new object[]{
									this.progname, pfound.Name };
								System.Console.Error.WriteLine(
									this.resManager.GetString(
									"getopt.arguments1", this.cultureInfo),
									msgArgs);
							}
							// +option or -option
							else
							{
								object[] msgArgs = new object[]{ this.progname,
									this.argv[optind - 1][0], pfound.Name};
								System.Console.Error.WriteLine(
									this.resManager.GetString(
									"getopt.arguments2", this.cultureInfo),
									msgArgs);
							}
						}
						
						this.nextchar = "";
						this.optopt = pfound.Val;
						
						return '?';
					}
				} // if (nameend)
				else if (pfound.HasArg == Argument.Required)
				{
					if (this.optind < this.argv.Length)
					{
						this.optarg = this.argv[this.optind];
						++this.optind;
					}
					else
					{
						if (this.opterr)
						{
							object[] msgArgs = new object[]{
								this.progname, this.argv[this.optind - 1]};
							System.Console.Error.WriteLine(
								this.resManager.GetString("getopt.requires",
								this.cultureInfo), msgArgs);
						}
						
						this.nextchar = "";
						this.optopt = pfound.Val;
						if (this.optstring[0] == ':')
							return ':';
						else
							return '?';
					}
				} // else if (pfound)
				
				this.nextchar = "";
				
				if (pfound.Flag != null)
				{
					pfound.Flag.Length = 0;
					pfound.Flag.Append(pfound.Val);
					
					return 0;
				}
				
				return pfound.Val;
			} // if (pfound != null)
			
			this.longoptHandled = false;
			
			return 0;
		}
		
		/// <summary>
		/// This method returns a char that is the current option that has been
		/// parsed from the command line. If the option takes an argument, then
		/// the internal variable <c>optarg</c> is set which is a string
		/// representing the the value of the argument. This value can be
		/// retrieved by the caller using the <see cref="Optarg"/> property. If
		/// an invalid option is found, an error message is printed and a
		/// '<c>?</c>' is returned.
		/// The name of the invalid option character can be retrieved by
		/// calling the <c>Optopt</c> property. When there are no more options
		/// to be scanned, this method returns -1. The index of first
		/// non-option element in argv can be retrieved with the
		/// <see cref="Optind"/> property.
		/// </summary>
		/// <returns>
		/// Various things as described above.
		/// </returns>
		public int getopt()	// not capitalized because of an compiler error
		{
			this.optarg = null;
			
			if (this.endparse == true)
				return -1;
			
			if ((this.nextchar == null) || (this.nextchar.Length == 0))
			{
				// If we have just processed some options following some
				// non-options, exchange them so that the options come first.
				if (this.lastNonopt > this.optind)
					this.lastNonopt = this.optind;
				if (this.firstNonopt > this.optind)
					this.firstNonopt = this.optind;
				
				if (this.ordering == Order.Permute)
				{
					// If we have just processed some options following some
					// non-options, exchange them so that the options come
					// first.
					if ((this.firstNonopt != this.lastNonopt) &&
						(this.lastNonopt != this.optind))
						this.exchange(this.argv);
					else if (this.lastNonopt != this.optind)
						this.firstNonopt = this.optind;
					
					// Skip any additional non-options
					// and extend the range of non-options previously skipped.
					while ((this.optind < this.argv.Length) &&
						((this.argv[optind].Length == 0) ||
						(this.argv[this.optind][0] != '-') ||
						this.argv[optind].Equals("-")))
                        this.optind++;
					
					this.lastNonopt = this.optind;
				}
				
				// The special ARGV-element "--" means premature end of
				// options. Skip it like a null option, then exchange with
				// previous non-options as if it were an option, then skip
				// everything else like a non-option.
				if ((this.optind != this.argv.Length) &&
					this.argv[this.optind].Equals("--"))
				{
					this.optind++;
					
					if ((this.firstNonopt != this.lastNonopt) &&
						(this.lastNonopt != this.optind))
						this.exchange(this.argv);
					else if (this.firstNonopt == this.lastNonopt)
						this.firstNonopt = this.optind;
					
					this.lastNonopt = this.argv.Length;
					
					this.optind = this.argv.Length;
				}
				
				// If we have done all the ARGV-elements, stop the scan
				// and back over any non-options that we skipped and permuted.
				if (this.optind == this.argv.Length)
				{
					// Set the next-arg-index to point at the non-options that
					// we previously skipped, so the caller will digest them.
					if (this.firstNonopt != this.lastNonopt)
						this.optind = this.firstNonopt;
					
					return -1;
				}
				
				// If we have come to a non-option and did not permute it,
				// either stop the scan or describe it to the caller and pass
				// it by.
				if ((this.argv[this.optind].Length == 0) ||
					(this.argv[this.optind][0] != '-') ||
					this.argv[this.optind].Equals("-"))
				{
					if (this.ordering == Order.RequireOrder)
						return -1;
					
					this.optarg = this.argv[optind++];
					return 1;
				}
				
				// We have found another option-ARGV-element.
				// Skip the initial punctuation.
				if (this.argv[optind].StartsWith("--"))
					this.nextchar = this.argv[this.optind].Substring(2);
				else
					this.nextchar = this.argv[this.optind].Substring(1);
			}
			
			// Decode the current option-ARGV-element.
			
			/*	Check whether the ARGV-element is a long option.
				
				If longOnly and the ARGV-element has the form "-f", where f is
				a valid short option, don't consider it an abbreviated form of
				a long option that starts with f. Otherwise there would be no
				way to give the -f short option.
				
				On the other hand, if there's a long option "fubar" and	the
				ARGV-element is "-fu", do consider that an abbreviation of the
				long option, just like "--fu", and not "-f" with arg "u".
				
				This distinction seems to be the most useful approach.
			*/
			if ((this.longOptions != null) &&
				(this.argv[this.optind].StartsWith("--") || (this.longOnly &&
				((this.argv[this.optind].Length > 2) ||
				(this.optstring.IndexOf(this.argv[this.optind][1]) == -1)))))
			{
				int c = this.checkLongOption();
				
				if (this.longoptHandled)
					return c;
				
				// Can't find it as a long option. If this is not
				// getopt_long_only, or the option starts with "--" or is not a
				// valid short option, then it's an error. Otherwise interpret
				// it as a short option.
				if (!this.longOnly || this.argv[this.optind].StartsWith("--")
					|| (this.optstring.IndexOf(this.nextchar[0]) == - 1))
				{
					if (this.opterr)
					{
						if (this.argv[this.optind].StartsWith("--"))
						{
							object[] msgArgs = new object[]{
								this.progname, this.nextchar };
							System.Console.Error.WriteLine(
								this.resManager.GetString("getopt.unrecognized",
								this.cultureInfo), msgArgs);
						}
						else
						{
							object[] msgArgs = new object[]{ this.progname,
								this.argv[optind][0], this.nextchar};
							System.Console.Error.WriteLine(
								this.resManager.GetString("getopt.unrecognized2",
								this.cultureInfo), msgArgs);
						}
					}
					
					this.nextchar = "";
					++this.optind;
					this.optopt = 0;
					
					return '?';
				}
			} // if (longopts)
			
			// Look at and handle the next short option-character */
			int c2 = this.nextchar[0]; //**** Do we need to check for empty str?
			if (this.nextchar.Length > 1)
				this.nextchar = this.nextchar.Substring(1);
			else
				this.nextchar = "";
			
			string temp = null;
			if (this.optstring.IndexOf((char) c2) != - 1)
				temp = this.optstring.Substring(
					this.optstring.IndexOf((char) c2));
			
			if (this.nextchar.Length == 0)
				++this.optind;
			
			if ((temp == null) || (c2 == ':'))
			{
				if (this.opterr)
				{
					if (this.posixlyCorrect)
					{
						// 1003.2 specifies the format of this message
						object[] msgArgs = new object[]{
							this.progname, (char) c2 };
						System.Console.Error.WriteLine(
							this.resManager.GetString("getopt.illegal",
							this.cultureInfo), msgArgs);
					}
					else
					{
						object[] msgArgs = new object[]{
							this.progname, (char) c2 };
						System.Console.Error.WriteLine(
							this.resManager.GetString("getopt.invalid",
							this.cultureInfo), msgArgs);
					}
				}
				
				this.optopt = c2;
				
				return '?';
			}
			
			// Convenience. Treat POSIX -W foo same as long option --foo
			if ((temp[0] == 'W') && (temp.Length > 1) && (temp[1] == ';'))
			{
				if (this.nextchar.Length != 0)
				{
					this.optarg = this.nextchar;
				}
				// No further cars in this argv element and no more argv
				// elements
				else if (this.optind == this.argv.Length)
				{
					if (this.opterr)
					{
						// 1003.2 specifies the format of this message. 
						object[] msgArgs = new object[]{
							this.progname, (char) c2 };
						System.Console.Error.WriteLine(
							this.resManager.GetString("getopt.requires2",
							this.cultureInfo), msgArgs);
					}
					
					this.optopt = c2;
					if (this.optstring[0] == ':')
						return ':';
					else
						return '?';
				}
				else
				{
					// We already incremented `optind' once; increment it again
					// when taking next ARGV-elt as argument. 
					this.nextchar = this.argv[this.optind];
					this.optarg = this.argv[this.optind];
				}
				
				c2 = this.checkLongOption();
				
				if (this.longoptHandled)
					return c2;
				// Let the application handle it
				else
				{
					this.nextchar = null;
					++this.optind;
					return 'W';
				}
			}
			
			if ((temp.Length > 1) && (temp[1] == ':'))
			{
				if ((temp.Length > 2) && (temp[2] == ':'))
				// This is an option that accepts and argument optionally
				{
					if (this.nextchar.Length != 0)
					{
						this.optarg = this.nextchar;
						++this.optind;
					}
					else
					{
						this.optarg = null;
					}
					
					this.nextchar = null;
				}
				else
				{
					if (this.nextchar.Length != 0)
					{
						this.optarg = this.nextchar;
						++this.optind;
					}
					else if (this.optind == this.argv.Length)
					{
						if (this.opterr)
						{
							// 1003.2 specifies the format of this message
							object[] msgArgs = new object[]{
								this.progname, (char) c2};
							System.Console.Error.WriteLine(
								this.resManager.GetString("getopt.requires2",
								this.cultureInfo), msgArgs);
						}
						
						this.optopt = c2;
						
						if (this.optstring[0] == ':')
							return ':';
						else
							return '?';
					}
					else
					{
						this.optarg = this.argv[this.optind];
						++this.optind;
						
						// Ok, here's an obscure Posix case.  If we have o:,
						// and we get -o -- foo, then we're supposed to skip
						// the --, end parsing of options, and make foo an
						// operand to -o. Only do this in Posix mode.
						if (this.posixlyCorrect && this.optarg.Equals("--"))
						{
							// If end of argv, error out
							if (this.optind == this.argv.Length)
							{
								if (this.opterr)
								{
									// 1003.2 specifies the format of this
									// message
									object[] msgArgs = new object[]{
										this.progname, (char) c2};
									System.Console.Error.WriteLine(
										this.resManager.GetString(
										"getopt.requires2", this.cultureInfo),
										msgArgs);
								}
								
								this.optopt = c2;
								
								if (this.optstring[0] == ':')
									return ':';
								else
									return '?';
							}
							
							// Set new optarg and set to end. Don't permute as
							// we do on -- up above since we know we aren't in
							// permute mode because of Posix.
							this.optarg = this.argv[this.optind];
							++this.optind;
							this.firstNonopt = this.optind;
							this.lastNonopt = this.argv.Length;
							this.endparse = true;
						}
					}
					
					this.nextchar = null;
				}
			}
			
			return c2;
		}
		#endregion
	} // Class Getopt
}