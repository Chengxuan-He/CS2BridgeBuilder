"""Install the current rail-free Lift05 continuous-road candidate."""
import sys
from PrepareCleanRoad import main as prepare, install


def main():
    prepare()
    install('--check' in sys.argv)


if __name__ == '__main__':
    main()
