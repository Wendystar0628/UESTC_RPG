import {
  Accessibility, Banknote, BedDouble, Bike, BookOpen, Building2, Bus, CircleDot,
  Coffee, Cross, DoorOpen, FlaskConical, Footprints, GraduationCap, Info,
  Landmark, MapPin, CircleHelp as MapPinHelp, Package, Route, School, SquareParking, Store,
  Trees, Trophy, Utensils, Waves,
} from 'lucide-react';

const icons = { Accessibility, Banknote, BedDouble, Bike, BookOpen, Building2, Bus, CircleDot, Coffee, Cross, DoorOpen, FlaskConical, Footprints, GraduationCap, Info, Landmark, MapPin, MapPinHelp, Package, Route, School, SquareParking, Store, Trees, Trophy, Utensils, Waves };

export default function PlaceIcon({ name, size = 19 }: { name: string; size?: number }) {
  const Icon = icons[name as keyof typeof icons] || MapPinHelp;
  return <Icon size={size} strokeWidth={1.7} aria-hidden="true" />;
}
