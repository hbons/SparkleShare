AC_DEFUN([SHAMROCK_EXPAND_LIBDIR],
[	
	expanded_libdir=`(
		case $prefix in 
			NONE) prefix=$ac_default_prefix ;; 
			*) ;; 
		esac
		case $exec_prefix in 
			NONE) exec_prefix=$prefix ;; 
			*) ;; 
		esac
		eval echo $libdir
	)`
	AC_SUBST(expanded_libdir)
])

AC_DEFUN([SHAMROCK_EXPAND_BINDIR],
[
	expanded_bindir=`(
		case $prefix in 
			NONE) prefix=$ac_default_prefix ;; 
			*) ;; 
		esac
		case $exec_prefix in 
			NONE) exec_prefix=$prefix ;; 
			*) ;; 
		esac
		eval echo $bindir
	)`
	AC_SUBST(expanded_bindir)
])

AC_DEFUN([SHAMROCK_EXPAND_DATADIR],
[
	case $prefix in
		NONE) prefix=$ac_default_prefix ;;
		*) ;;
	esac

	case $exec_prefix in
		NONE) exec_prefix=$prefix ;;
		*) ;;
	esac

	expanded_datadir=`(eval echo $datadir)`
	expanded_datadir=`(eval echo $expanded_datadir)`

	AC_SUBST(expanded_datadir)
])

