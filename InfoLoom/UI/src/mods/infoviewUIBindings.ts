import { ValueBinding, bindValue} from "cs2/api" 
import { IndicatorValue } from "cs2/bindings";
import {Entity} from "cs2/utils"







//Current Student Count Bindings
export const elementaryStudentCount$: ValueBinding<number> = bindValue<number>("InfoLoomTwo", "elementaryStudentCount", 0);
export const highSchoolStudentCount$: ValueBinding<number> = bindValue<number>("InfoLoomTwo", "highSchoolStudentCount", 0);
export const collegeStudentCount$: ValueBinding<number> = bindValue<number>("InfoLoomTwo", "collegeStudentCount", 0);
export const universityStudentCount$: ValueBinding<number> = bindValue<number>("InfoLoomTwo", "universityStudentCount", 0);

//Student Eligibility Bindings
export const elementaryEligible$: ValueBinding<number> = bindValue<number>("InfoLoomTwo", "elementaryEligible", 0);
export const highSchoolEligible$: ValueBinding<number> = bindValue<number>("InfoLoomTwo", "highSchoolEligible", 0);
export const collegeEligible$: ValueBinding<number> = bindValue<number>("InfoLoomTwo", "collegeEligible", 0);
export const universityEligible$: ValueBinding<number> = bindValue<number>("InfoLoomTwo", "universityEligible", 0);

//Student Capacity Bindings
export const elementaryCapacity$: ValueBinding<number> = bindValue<number>("InfoLoomTwo", "elementaryCapacity", 0);
export const highSchoolCapacity$: ValueBinding<number> = bindValue<number>("InfoLoomTwo", "highSchoolCapacity", 0);
export const collegeCapacity$: ValueBinding<number> = bindValue<number>("InfoLoomTwo", "collegeCapacity", 0);
export const universityCapacity$: ValueBinding<number> = bindValue<number>("InfoLoomTwo", "universityCapacity", 0);

//Indicator Values
export const elementaryAvailability$: ValueBinding<IndicatorValue> = bindValue<IndicatorValue>("InfoLoomTwo", "elementaryAvailability");
export const highSchoolAvailability$: ValueBinding<IndicatorValue> = bindValue<IndicatorValue>("InfoLoomTwo", "highSchoolAvailability");
export const collegeAvailability$: ValueBinding<IndicatorValue> = bindValue<IndicatorValue>("InfoLoomTwo", "collegeAvailability");
export const universityAvailability$: ValueBinding<IndicatorValue> = bindValue<IndicatorValue>("InfoLoomTwo", "universityAvailability");